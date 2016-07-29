using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;
using System.Collections;
using System.Reflection;
using ExtendedIndexBTree;
using System.IO;
using Common;

namespace ORMPolar
{
    public interface IDbSet : IDisposable
    {
        bool CheckUniqueness(string fieldName, object value);
        void Clear();
    }

    /// <summary>
    /// Класс, представляющий набор сущностей, хранящихся в БД
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class DbSet<TEntity> : IDbSet where TEntity : Entity, new()
    {
        private static PaCell _cell;
        private Dictionary<string, IIndex> _indexDictionary = new Dictionary<string, IIndex>();

        private void CheckСommunication(string target, string field, object value)
        {
            IDbSet DbSet;
            DbContext.dbSets.TryGetValue(target, out DbSet);
            bool notexist = DbSet.CheckUniqueness(field, value);
            if (notexist)
                throw new Exception("нарушается целостность данных");
        }

        public bool CheckUniqueness(string fieldName, object value)
        {
            //if (_cell.IsEmpty)
            //    return;
            IIndex index;
            _indexDictionary.TryGetValue(fieldName, out index);
            if (((PxCell)index.IndexCell).Root.Tag() == 0)
                return true;
            var offsets = index.FindAll(value);
            ///////////////////////////////////////////////////////////////////////////////////////////////////
            foreach(var offset in offsets)
            {
                PaEntry entry = _cell.Root.Element(0);
                entry.offset = offset;
                var isDeleted = (bool)((object[])entry.Get())[0];
                if (!isDeleted)
                    return false;
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////
            return true;
        }

        public void Delete(ref TEntity entity)
        {
            BeforeDelete(entity);
            PaEntry entry = _cell.Root.Element(0);
            entry.offset = entity.offset;
            entry.Field(0).Set(true);
            entity = null;
        }

        public void Sync(TEntity entity)// Warning: нельзя менять ключевые поля
        {
            //TODO: проверить менялось ли ключевое поле
            PaEntry entry = _cell.Root.Element(0);
            entry.offset = entity.offset;
            entry.Field(0).Set(true);
            Append(entity);
        }

        #region Comparers
        private Func<object, object, int> stringElementComparer(int n)
        {
            return ((object ob1, object ob2) =>
            {
                object[] node1 = (object[])ob1;
                int hash1 = (int)node1[1];

                object[] node2 = (object[])ob2;
                int hash2 = (int)node2[1];

                if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
                {
                    PaEntry entry = _cell.Root.Element(0);
                    long offset1 = (long)node1[0];
                    long offset2 = (long)node2[0];

                    entry.offset = offset1;
                    object[] pair1 = (object[])entry.Get();
                    string key1 = (string)pair1[n]; 

                    entry.offset = offset2;
                    object[] pair2 = (object[])entry.Get();
                    string key2 = (string)pair2[0];

                    return String.Compare(key1, key2, StringComparison.Ordinal);//вернётся: -1,0,1
                }
                else
                    return ((hash1 < hash2) ? -1 : 1);

            });
        }

        private Func<object, object, int> intElementComparer = (object ob1, object ob2) =>
        {
            object[] node1 = (object[])ob1;
            int value1 = (int)node1[1];

            object[] node2 = (object[])ob2;
            int value2 = (int)node2[1];

            if (value1 == value2) return 0;

            return ((value1 < value2) ? -1 : 1);
        };

        private Func<object, object, int> intKeyComparer = (object ob1, object ob2) =>
        {
            int value1 = (int)ob1;

            object[] node2 = (object[])ob2;
            int value2 = (int)node2[1];

            if (value1 == value2) return 0;

            return ((value1 < value2) ? -1 : 1);
        };

        private Func<object, object, int> stringKeyComparer(int n) 
        {
            return ((object ob1, object ob2) =>
            {
                string value1 = (string)ob1;
                int hash1 = value1.GetHashCode();

                object[] node2 = (object[])ob2;
                int hash2 = (int)node2[1];

                if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
                {
                    PaEntry entry = _cell.Root.Element(0);
                    entry.offset = (long)node2[0];
                    object[] pair2 = (object[])entry.Get();
                    string value2 = (string)pair2[n];

                    return String.Compare(value1, value2, StringComparison.Ordinal);//вернётся: -1,0,1
                }
                else
                    return ((hash1 < hash2) ? -1 : 1);
            });
        }
        #endregion

        PropertyInfo[] fields;
        List<string> fieldNames;
        public DbSet() //TODO: необходим рефакторинг
        {
            Type entityType = typeof(TEntity);
            DbContext.sTables.TryGetValue(entityType, out _cell);

            fields = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            fieldNames = new List<string>();
            int num = 0;
            foreach (var field in fields)
            {
                fieldNames.Add(field.Name);
                Attribute[] attr = field.GetCustomAttributes().ToArray();

                if (attr.Count() == 0)
                    continue;

                for(int i=0; i < attr.Count(); ++i)
                {
                    if ((attr[i] is IndexAttribute))
                    {

                        string path;
                        DbContext.sTablePaths.TryGetValue(entityType, out path);
                        IIndex bTreeInd = createIndex(field, num, path);

                        _indexDictionary.Add(field.Name, bTreeInd);

                        if (field.PropertyType == typeof(string))
                            OnAppendElement += (off, ent) =>
                            {
                                var _field = field;
                                bTreeInd.AppendElement(new object[] { off, _field.GetValue(ent).GetHashCode() });
                            };
                        else
                            OnAppendElement += (off, ent) =>
                            {
                                var _field = field;
                                bTreeInd.AppendElement(new object[] { off, _field.GetValue(ent) });
                            };
                    }
                    SetRelationChecks(attr[i], field);
                }
                ++num;
            }

            //зарегистрирует таблицу в DbContext (нужно для реализации связей)
            DbContext.dbSets.Add(entityType.Name, this);
        }

        private void SetRelationChecks(Attribute attr, PropertyInfo field)
        {
            if (attr is ManyToOneAttribute)
            {
                var target = (attr as RelationAttribute).target;
                var foreighnField = (attr as RelationAttribute).foreighnField;
                BeforeAppend += (TEntity ent) =>
                {
                    var _target = target;
                    var _foreighnField = foreighnField;
                    var _field = field;
                    var _value = _field.GetValue(ent);
                    CheckСommunication(_target, _foreighnField, _value);
                };
            }
            else if (attr is OneToManyAttribute)
            {
                BeforeAppend += (TEntity ent) =>
                {
                    var _field = field;
                    var Name = _field.Name;
                    var Value = _field.GetValue(ent);
                    if (!CheckUniqueness(Name, Value))
                        throw new Exception("нарушается целостность данных, вставка не уникального ключа");
                };
                var target = (attr as RelationAttribute).target;
                var foreighnField = (attr as RelationAttribute).foreighnField;
                BeforeDelete += (TEntity ent) =>
                {
                    var _target = target;
                    var _foreighnField = foreighnField;
                    var _field = field;
                    var _value = _field.GetValue(ent);

                    IDbSet DbSet;
                    DbContext.dbSets.TryGetValue(_target, out DbSet);
                    bool exist = !DbSet.CheckUniqueness(_foreighnField, _value);
                    if (exist)
                        throw new Exception("нарушается целостность данных, есть зависимые данные");
                };
            }
            else if (attr is OneToOneAttribute)
            {
                //TODO:
            }
        }


        private IIndex createIndex(PropertyInfo field, int num, string path)
        {
            Type typeField = field.PropertyType;

            NamedType offset = new NamedType("offset", new PType(PTypeEnumeration.longinteger));
            NamedType key;

            if (typeField == typeof(long))
                key = new NamedType("key", new PType(PTypeEnumeration.longinteger));
            else
                key = new NamedType("key", new PType(PTypeEnumeration.integer));

            PType bTreeType = new PTypeRecord(offset, key);

            IIndex bTreeInd;

            if (typeField == typeof(string))
            {
                bTreeInd = new BTreeInd(bTreeType,
                stringKeyComparer(num+1), //т.к. 0е поле для удаления
                stringElementComparer(num+1),
                Path.GetDirectoryName(path)+"/Index[" + Path.GetFileNameWithoutExtension(path) + "]-[" + field.Name + "].pxc");
            }
            else if (typeField == typeof(int))
            {
                bTreeInd = new BTreeInd(bTreeType,
                intKeyComparer,
                intElementComparer,
                Path.GetDirectoryName(path) + "/Index[" + Path.GetFileNameWithoutExtension(path) + "]-[" + field.Name + "].pxc");
            }
            else
                throw new NotImplementedException();// TODO: реализовать для других типов

            return bTreeInd;
        }

        public void Clear()
        {
            _cell.Clear();
            _cell.Fill(new object[0]);
        }

        /// <summary>
        /// Добавление сущности
        /// </summary>
        /// <param name="entity"></param>
        public void Append(TEntity entity)
        {
            BeforeAppend(entity); //проверяет не нарушается ли целостность БД, кидает исключение, если да
            var e = fields
                    .Select<PropertyInfo, object>(propertyInfo => propertyInfo.GetValue(entity))
                    .ToArray<object>();
            long offset = _cell.Root.AppendElement(
                new object[]{ false }.Concat<object>(e).ToArray<object>()
            );
            typeof(Entity).GetProperty("offset").SetValue(entity, offset);

            OnAppendElement(offset, entity);
        }

        public event Action<long, TEntity> OnAppendElement = (long o, TEntity entity) => { };
        public event Action<TEntity> BeforeAppend = (TEntity entity) => { };
        public event Action<TEntity> BeforeDelete = (TEntity entity) => { };

        public void Flush()
        {
            _cell.Flush();
        }

        public PaCell Get()
        {
            return _cell;
        }

        /// <summary>
        /// метод преобразования object к типу Entity
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private TEntity Convert(object[] element, long offset)
        {
            TEntity entity = new TEntity();
            if ((bool)element[0])
                return null;
            int t = 1; //0 - для поля deleted
            foreach (var rec in fields)
            {
                rec.SetValue(entity,element[t]);
                ++t;
            }
            typeof(Entity).GetProperty("offset").SetValue(entity, offset);

            return entity;
        }

        public IEnumerable<TEntity> Elements()
        {
            return _cell.Root.Elements()
                .Select<PaEntry, TEntity>(en => Convert((object[])en.Get(), en.offset));
        }

        /// <summary>
        /// Метод поиска всех вхождений ключа
        /// </summary>
        /// <param name="fieldName">имя поля, по которому ведётся поиск</param>
        /// <param name="key">искомый ключ</param>
        /// <returns>набор сущностей</returns>
        public IEnumerable<TEntity> FindAll(string fieldName, object key)
        {
            IIndex index;
            _indexDictionary.TryGetValue(fieldName, out index);

            PaEntry entry = _cell.Root.Element(0);

            return index.FindAll(key)
                .Select<long, TEntity>(
                off =>
                {
                    entry.offset = off;

                    return Convert((object[])entry.Get(), off);
                }
                );
        }

        /// <summary>
        /// Метод поиска первого вхождения ключа
        /// </summary>
        /// <param name="fieldName">имя поля, по которому ведётся поиск</param>
        /// <param name="key">искомый ключ</param>
        /// <returns>найденная сущность</returns>
        public TEntity FindFirst(string fieldName, object key)
        {
            IIndex index;
            _indexDictionary.TryGetValue(fieldName, out index);
            if (index==null)
                return null; //TODO: искать полным перебором?
            PaEntry entry = _cell.Root.Element(0);
            long offset = index.FindFirst(key); //TODO: если из индекса не удаляются значения, то искать всегда через FindAll
            entry.offset = offset;
            if (offset < 0) 
                return null;
            return Convert((object[])entry.Get(), offset);
        }

        public void Dispose()
        {
            foreach(var index in _indexDictionary.Values)
            {
                index.Dispose();
            }
        }

        //private void OnPropertyChanged(object value, long offset, string propertyName)
        //{
        //    PaEntry entry = _cell.Root.Element(0);
        //    entry.offset = offset;
        //    int indexField = fieldNames.IndexOf(propertyName);
        //    entry.Field(indexField).Set(value);
        //}
    }
}
