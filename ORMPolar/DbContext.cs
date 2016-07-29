using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using PolarDB;
using System.Xml.Linq;

namespace ORMPolar
{
    // TODO: оставить регистрацию ячеек и реализовать поддержку связей, остальной код перенести в DbSet
    public class DbContext: IDisposable
    {
        private static DbContext instance;
        private XDocument schema;

        public static Dictionary<Type, PaCell> sTables = new Dictionary<Type, PaCell>();
        public static Dictionary<Type, string> sTablePaths = new Dictionary<Type, string> ();
        public static Dictionary<string, IDbSet> dbSets = new Dictionary<string, IDbSet>();

        public static DbContext GetInstance()
        {
            if (instance == null)
                throw new Exception("Can't create DbContext without path to a schema");
            else
                return instance;
        }

        public static DbContext GetInstance(string schemaPath)
        {
            if (instance == null)
                instance = new DbContext(schemaPath);
            return instance;
        }

        protected DbContext(string schemaPath)
        {
            //Достаем путь до ячейки таблицы из xml конфига
            schema = XDocument.Load(schemaPath);
            var nameSpace = Assembly.GetExecutingAssembly().GetName().Name;
            foreach (XElement element in schema.Root.Elements()
                    .Where(el => el.Name == "class"))
            {
                Type t = Type.GetType(nameSpace + "." + element.Attribute("name").Value);
                CreateTable(t, element.Attribute("path").Value);
            }            
        }

        private PType GetPolarType(Type t)
        {
            if (t == typeof(string)) return new PType(PTypeEnumeration.sstring);
            if (t == typeof(int)) return new PType(PTypeEnumeration.integer);
            if (t == typeof(long)) return new PType(PTypeEnumeration.longinteger);
            if (t == typeof(bool)) return new PType(PTypeEnumeration.boolean);
            if (t == typeof(double) || (t == typeof(float))) return new PType(PTypeEnumeration.real);
            throw new Exception("Error type");
        }

        private void CreateTable(Type type, string fullName)
        {
            //получаем все поля
            var fields = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            NamedType[] temp =
                fields
                    .Select<PropertyInfo, NamedType>(propertyInfo =>
                        new NamedType(propertyInfo.Name, GetPolarType(propertyInfo.PropertyType)))
                    .ToArray<NamedType>();

            PType typeCell = new PTypeSequence(
                new PTypeRecord(
                    new NamedType[] { new NamedType("deleted", new PType(PTypeEnumeration.boolean)) }.
                    Concat<NamedType>(temp).ToArray<NamedType>()
                )
            );
            PaCell table = new PaCell(typeCell, fullName, false);
            sTables.Add(type,table);
            sTablePaths.Add(type, fullName);
        }

        virtual public void Dispose()
        {
            foreach (var val in sTables.Values)
            {
                val.Close();
            }
        }
    }
}
