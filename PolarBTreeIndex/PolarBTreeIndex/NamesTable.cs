using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PolarBtreeIndex
{
    class NamesTable
    {
        private string path;
        private PType tp, BTreeType;
        public static PaCell tableNames;
        public PaCell index;
        private BTreeInd BTreeInd;

        private static Func<object, object, int> compareName = (object ob1, object ob2) =>
        {
            return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
        };

        //Компаратор для офсетов на строки
        private Func<object, object, int> elementComparer = (object ob1, object ob2) =>
        {
            object[] node1 = (object[])ob1;
            int hash1 = (int)node1[1];

            object[] node2 = (object[])ob2;
            int hash2 = (int)node2[1];

            if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
            {
                PaEntry entry = tableNames.Root.Element(0);
                long offset1 = (long)node1[0];
                long offset2 = (long)node2[0];

                object[] pair1 = (object[])entry.Get();
                entry.offset = offset1;
                string name1 = (string)pair1[1];
                entry.offset = offset2;
                object[] pair2 = (object[])entry.Get();
                string name2 = (string)pair2[1];

                return String.Compare(name1, name2, StringComparison.Ordinal);//вернётся: -1,0,1
            }
            else
                return ((hash1 < hash2) ? -1 : 1);

        };

        public NamesTable(string path)
        {
            this.path = path;

            //задаём тип для записи в ячейку БД
            tp = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.longinteger)),
                new NamedType("string", new PType(PTypeEnumeration.sstring)),
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)))
            );

            if (!System.IO.File.Exists(path + "TableOfNames.pac"))
            {
                //создаём БД
                tableNames = new PaCell(tp, path + "TableOfNames.pac", false);

                //очистка БД
                tableNames.Clear();
                tableNames.Fill(new object[0]);
            }

            //типы ячеек индекса в виде последовательности
            PType tp_id = new PTypeSequence(new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("id", new PType(PTypeEnumeration.longinteger)))
            );

            //узел дерева состоит из офсета и хешкода
            BTreeType = new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("hash", new PType(PTypeEnumeration.integer))
            );

            //Индексы
            index = new PaCell(tp_id, path + "SimpleIndex.pac", false);
            BTreeInd = new BTreeInd(50,BTreeType, elementComparer, path + "BTreeIndex.pax");
        }

        public void MakeIndex()
        {
            index.Clear();
            index.Fill(new object[0]);

            foreach (PaEntry ent in tableNames.Root.Elements())
                    //.Where(ent => (bool)ent.Field(2).Get() == false))
            {
                if ((bool)ent.Field(2).Get() == false)
                {
                    index.Root.AppendElement(new object[] { ent.offset, ent.Field(0).Get() });

                    var hash = ent.Field(1).Get().GetHashCode();
                    BTreeInd.Add(new object[] { ent.offset, hash });
                }

            }
            index.Flush();

            //Сортировка офсетов по id строк
            index.Root.SortByKey<long>(ob => (long)((object[])ob)[1]);
        }

        public void Add(string str)
        {
            long newCode = tableNames.Root.Count();
            long offset = tableNames.Root.AppendElement(new object[] { newCode, str, false });
        }

        public void LoadTable(uint portion, uint numberPortion)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random();

            for (uint i = 0; i < numberPortion; i++)
            {
                HashSet<string> hs = new HashSet<string>();

                for (uint j = 0; j < portion; j++)
                {
                    string s = "s" + rnd.Next(10000000);
                    hs.Add(s);
                }
                string[] arr = hs.ToArray();
                Array.Sort<string>(arr);

                sw.Start();
                InsertPortion(arr);
                sw.Stop();
            }
            Console.WriteLine("Загрузка закончена. Время={0}", sw.ElapsedMilliseconds);
        }

        public void InsertPortion(string[] sortedArray)
        {
            Dictionary<string, long> dictionary = new Dictionary<string,long>();

            if (sortedArray.Length == 0)
                return;
            bool portionIsOver = false;

            int indexName = 0;
            string nameFromPortion = sortedArray[indexName];

            long newCode = tableNames.Root.Count();

            if (System.IO.File.Exists(path + "temp.pac"))
                System.IO.File.Delete(path + "temp.pac");
            PaCell temp = new PaCell(tp, path + "temp.pac", false);

            temp.Clear();
            temp.Fill(new object[0]);

            int cmp;
            // добавляем прежние и новые элементы в вспомогательную ячейку с сохранением сортировки
            foreach (object[] pair in tableNames.Root.ElementValues())
            {
                string name = (string)pair[1];
                while (!portionIsOver && ((cmp = nameFromPortion.CompareTo(name)) <= 0))
                {
                    if (cmp < 0)
                    {
                        // используем новый код
                        object[] newPair = new object[] { newCode, nameFromPortion, false };
                        temp.Root.AppendElement(newPair);
                        newCode++;
                        dictionary.Add((string)newPair[1], (long)newPair[0]);
                    }
                    else
                    {
                        dictionary.Add((string)pair[1], (long)pair[0]);
                    }
                    ++indexName;
                    if (indexName < sortedArray.Length)
                        nameFromPortion = sortedArray[indexName];
                    else
                        portionIsOver = true;
                }
                temp.Root.AppendElement(pair); // переписывается тот же объект
            }
            // добавляем остальные элементы
            while (indexName < sortedArray.Length)
            {
                nameFromPortion = sortedArray[indexName++];
                object[] newPair = new object[] { newCode, nameFromPortion, false };
                temp.Root.AppendElement(newPair);
                newCode++;
                dictionary.Add((string)newPair[1], (long)newPair[0]);
            }

            temp.Flush();
            temp.Close();
            tableNames.Close();

            System.IO.File.Delete(path + "TableOfNames.pac");
            System.IO.File.Move(path + "temp.pac", path + "TableOfNames.pac");

            tableNames = new PaCell(tp, path + "TableOfNames.pac", false);
        }

        public void Warmup()
        {
            foreach (var v in tableNames.Root.ElementValues()) ;
            foreach (var v in index.Root.ElementValues()) ;
            //TODO: Разогрев дерева?
        }

        private PaEntry SearchString(string srch)
        {
            PaEntry entry = tableNames.Root.Element(0);
            int hash = srch.GetHashCode();

            //PxEntry found = BTreeInd.BinarySearch(pe =>
            //{
            //    object[] node = (object[])pe.Get();

            //    if (hash == (int)node[1])
            //    {
            //        entry.offset = (long)node[0];
            //        object getStr = entry.Field(1).Get();
            //        int resultCompare = String.Compare((string)getStr, srch, StringComparison.Ordinal); //srch.CompareTo(getStr);
            //        return resultCompare;
            //    }
            //    else
            //        return ((hash < (int)node[1]) ? -1 : 1);
            //});

            //if (!found.IsEmpty)
            //{
            //    entry.offset = (long)((object[])found.Get())[0];
            //    bool isDeleted = (bool)entry.Field(2).Get();
            //    if (!isDeleted)
            //    {
            //        return entry;
            //    }
            //}

            entry.offset = Int64.MinValue;
            return entry;
        }

        public long? GetIdByString(string srchStr)
        {
            PaEntry ent;

            if (tableNames.Root.Count() == 0)
                return null;

            ent = SearchString(srchStr);

            if (ent.IsEmpty)
                return null;

            object[] pair = (object[])ent.Get();

            return (long)pair[0];
        }

        public string GetStringById(int id)
        {
            if (id < 0 || id > tableNames.Root.Count() || index.IsEmpty)
                throw new Exception("Строки с таким id нет");

            if (tableNames.Root.Count() == 0 || tableNames.IsEmpty)
                throw new Exception("Таблица имен пуста");

            PaEntry ent = tableNames.Root.Element(0);
            ent.offset = (long)index.Root.Element(id).Field(0).Get();
            return (string)ent.Field(1).Get();
        }

        public long GetCount()
        {
            return tableNames.Root.Count();
        }

        public void WriteTreeInFile(string path)
        {
            BTreeInd.WriteTreeInFile(path);
        }

        public void Dispose()
        {
            index.Close();
            BTreeInd.Close();
            tableNames.Close();
        }
        public void Delete()
        {
            System.IO.File.Delete(path + "SimpleIndex.pac");
            System.IO.File.Delete(path + "BTreeIndex.pax");
            System.IO.File.Delete(path + "TableOfNames.pac");
        }

    }
}
