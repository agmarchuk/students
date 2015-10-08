using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Collections;
using System.Reflection;
using ExtendedIndexBTree;
using System.Xml.Linq;
using System.IO;

namespace ORMPolar
{
    public class DbSet<TEntity>: IEnumerable<TEntity>, IDisposable where TEntity:class, new()
    {
        private static PaCell _cell;
        private Dictionary<string, IIndex> _indexDictionary = new Dictionary<string, IIndex>();

        private Func<object, object, int> stringElementComparer = (object ob1, object ob2) =>
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
                string key1 = (string)pair1[1];

                entry.offset = offset2;
                object[] pair2 = (object[])entry.Get();
                string key2 = (string)pair2[1];

                return String.Compare(key1, key2, StringComparison.Ordinal);//вернётся: -1,0,1
            }
            else
                return ((hash1 < hash2) ? -1 : 1);

        };

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

        private Func<object, object, int> stringKeyComparer = (object ob1, object ob2) =>
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
                string value2 = (string)pair2[1];

                return String.Compare(value1, value2, StringComparison.Ordinal);//вернётся: -1,0,1
            }
            else
                return ((hash1 < hash2) ? -1 : 1);
        };


        public DbSet()
        {
            TEntity entity = new TEntity();
            Type entityType = entity.GetType();
            DbContext.sTables.TryGetValue(entity.GetType(), out _cell);

            var fields = entityType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                Attribute[] attr = field.GetCustomAttributes().ToArray();

                if (attr.Count()>0)

                   for(int i=0; i < attr.Count(); ++i)
                    {
                        if (attr[i] is IndexAttribute)
                        {
                            Type typeField = field.FieldType;

                            NamedType offset = new NamedType("offset", new PType(PTypeEnumeration.longinteger));
                            NamedType key;
                            

                            if (typeField == typeof(long))
                                key = new NamedType("key", new PType(PTypeEnumeration.longinteger));
                            else
                                key = new NamedType("key", new PType(PTypeEnumeration.integer));

                            PType bTreeType = new PTypeRecord(offset, key);

                            string path;
                            DbContext.sTablePaths.TryGetValue(entityType, out path);

                            IIndex bTreeInd;

                            if (typeField == typeof(string))
                            {
                                bTreeInd = new BTreeInd(bTreeType,
                                stringKeyComparer,
                                stringElementComparer,
                                //Path.GetDirectoryName(path) +
                                //@"/media/data/My_Documents/Coding/_VSprojects/students/TestPlatform/bin/Release/"+
                                "Index[" + Path.GetFileNameWithoutExtension(path) + "]-[" + field.Name + "].pxc");
                            }
                            else
                            {
                                bTreeInd = new BTreeInd(bTreeType,
                                intKeyComparer,
                                intElementComparer,
                                //Path.GetDirectoryName(path) +
                                //@"/media/data/My_Documents/Coding/_VSprojects/students/TestPlatform/bin/Release/" +
                                "Index[" + Path.GetFileNameWithoutExtension(path) + "]-[" + field.Name + "].pxc");
                            }
                            
                            _indexDictionary.Add(field.Name, bTreeInd);

                            if (typeField == typeof(string))
                                OnAppendElement += (off, ent) => bTreeInd.AppendElement(new object[] { off, field.GetValue(ent).GetHashCode() }); 
                            else
                                OnAppendElement += (off, ent) => bTreeInd.AppendElement(new object[] { off, field.GetValue(ent) });
                        }

                    }
            }
        }

        //TODO: Для красоты использования можно реализовать интерфейс IEnumerable<TEntity>
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _cell.Clear();
            _cell.Fill(new object[0]);
        }
        public void Append(TEntity entity)
        {
            var fields = entity.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            long offset = _cell.Root.AppendElement(
                fields
                    .Select<FieldInfo, object>(fieldInfo => fieldInfo.GetValue(entity))
                    .ToArray<object>()
            );

            OnAppendElement(offset, entity);
        }

        public event Action<long, TEntity> OnAppendElement = (long o, TEntity entity) => { };

        public void Flush()
        {
            _cell.Flush();
        }

        public PaCell Get()
        {
            return _cell;
        }

        private TEntity Convert(object[] element)
        {
            TEntity entity = new TEntity();
            var fields = entity.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            int t = 0;
            foreach (var rec in fields)
            {
                rec.SetValue(entity,element[t]);
                ++t;
            }

            return entity;
        }

        public IEnumerable<TEntity> Elements()
        {
            return _cell.Root.Elements()
                .Select<PaEntry, TEntity>(en => Convert((object[])en.Get()));
        }

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
                    return Convert((object[])entry.Get());
                }
                );
        }
        public TEntity FindFirst(string fieldName, object key)
        {
            IIndex index;
            _indexDictionary.TryGetValue(fieldName, out index);

            PaEntry entry = _cell.Root.Element(0);


            entry.offset = index.FindFirst(key);

            if (entry.offset < 0) return null;
            
            return Convert((object[])entry.Get());
        }

        public void Dispose()
        {
            foreach(var index in _indexDictionary.Values)
            {
                index.Dispose();
            }
        }
    }
}
