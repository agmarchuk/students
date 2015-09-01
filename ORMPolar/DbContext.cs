﻿using System;
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
    /// <summary>
    /// Класс реализует взаимосвязь между классами и таблицами БД
    /// </summary>
    public class DbContext
    {
        private static DbContext instance;
        private XDocument schema;

        public static Dictionary<Type, PaCell> stables = new Dictionary<Type,PaCell>();

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
            if (t == typeof(double)) return new PType(PTypeEnumeration.real);
            throw new Exception("Error type");
        }

        private void CreateTable(Type type, string fullName)
        {
            //получаем все поля
            var fields = type.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            
            PType typeCell = new PTypeSequence(
                new PTypeRecord(
                fields
                    .Select<FieldInfo, NamedType>(fieldInfo => 
                        new NamedType(fieldInfo.Name, GetPolarType(fieldInfo.FieldType)))
                    .ToArray<NamedType>()
                )
            );
            PaCell table = new PaCell(typeCell,fullName,false);
            stables.Add(type,table);
        }

        public void SaveChanges()
        {
 
        }
  
   
    }
}