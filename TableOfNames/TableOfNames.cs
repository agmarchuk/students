using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableOfNames
{
    class TableOfNames
    {
        private string path;

        private PType tp;
        private PaCell tableNames, index;
        private BinaryTreeIndex binTreeInd;

        private PaEntry SearchString(string srch)
        {
            PaEntry entry = tableNames.Root.Element(0);
            var get = (long)binTreeInd.Root.UElement().Field(0).Get();

            //Console.WriteLine("TreeRoot: {0}", get);

            PxEntry found = binTreeInd.BinarySearch(pe =>
            {
                entry.offset = (long)pe.Get();

                object getStr = entry.Field(1).Get();

                //Console.WriteLine("Found: {0}",getStr);

                int resultCompare = String.Compare((string)getStr, srch, StringComparison.Ordinal); //srch.CompareTo(getStr);
                
                return resultCompare;
            });

            if (!found.IsEmpty) 
                entry.offset = (long)found.Get();
            else 
                entry.offset = Int64.MinValue;

            return entry;
        }

        public long GetCount()
        {
            return tableNames.Root.Count();
        }

        public void TreeShow()
        {
            var rez = binTreeInd.Root.GetValue();
            Console.WriteLine(rez.Type.Interpret(rez.Value));
        }

        public long? GetIdByString(string srchStr) 
        {
            PaEntry ent = SearchString(srchStr);

            if (ent.IsEmpty) 
                return null;

            object[] pair = (object[])ent.Get();

            return (long)pair[0]; 
        }

        public string GetStringById(int id)
        {
            if (id < 0 || id > tableNames.Root.Count() || index.IsEmpty)
                throw new Exception("Строки с таким id нет");

            if (tableNames.Root.Count()==0 || tableNames.IsEmpty)
                throw new Exception("Таблица имен пуста");

            PaEntry ent = tableNames.Root.Element(0);
            ent.offset = (long)index.Root.Element(id).Field(0).Get();
            return (string)ent.Field(1).Get();
        }

        public TableOfNames(string path)
        {
            this.path = path;

            //задаём тип для записи в ячейку БД
            tp = new PTypeSequence(new PTypeRecord(
                new NamedType("id",new PType(PTypeEnumeration.longinteger)),
                new NamedType("string",new PType(PTypeEnumeration.sstring)))
            );

            //создаём БД
            tableNames = new PaCell(tp, path + "TableOfNames.pac", false);

            //очистка БД
            tableNames.Clear();
            tableNames.Fill(new object[0]);

            //типы ячеек для таблицы с офсетами и для таблицы "offset-id"
            PType tp_of = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
            PType tp_id = new PTypeSequence(new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("id", new PType(PTypeEnumeration.longinteger)))
            );

            index = new PaCell(tp_id, path + "index.pac", false);

            PType binTreeType = new PType(PTypeEnumeration.longinteger);

            Func<object, object, int> elementCompare = (object ob1, object ob2) =>
            {
                PaEntry entry = tableNames.Root.Element(0);
                long offset1 = (long)(ob1);
                long offset2 = (long)(ob2);

                object[] pair1 = (object[])entry.Get();
                entry.offset = offset1;
                string name1 = (string)pair1[1];
                entry.offset = offset2;
                object[] pair2 = (object[])entry.Get();
                string name2 = (string)pair2[1];

                return String.Compare(name1, name2, StringComparison.Ordinal);//вернётся: -1,0,1
            };
            binTreeInd = new BinaryTreeIndex(binTreeType, elementCompare, path + "IndexNames.pac",false);
        }

        public void SlowCreateIndex()
        {
            index.Clear();
            index.Fill(new object[0]);

            foreach (PaEntry ent in tableNames.Root.Elements())
            {
                index.Root.AppendElement(new object[] { ent.offset, ent.Field(0).Get() });
                binTreeInd.Add(ent.offset);

                //Console.WriteLine("offset на строку в опорной таблице: {0}", ent.offset);
               // Console.WriteLine("Количество узлов " + binTreeInd.Root.Elements().Count<PTypeUnion>(binTreeInd.Root.Elements()));
            }
            index.Flush();

            //Сортировка офсетов по id строк
            index.Root.SortByKey<long>(ob => (long)((object[])ob)[1]);
        }



        public void CreateIndex()
        {
            if (tableNames.Root.Count() == 0) return;

            index.Clear(); index.Fill(new object[0]);
            binTreeInd.Clear(); 
            tableNames.Root.Scan((off, ob) =>
            {
                object[] pair = (object[])ob;
                index.Root.AppendElement(new object[] { off, (long)pair[0] });

                Console.WriteLine("offset на строку в опорной таблице: {0}", off);

                binTreeInd.Add(off);//повторяются оффсеты, возможно баг?!
                return true;
            });
            index.Flush();

            //Сортировка офсетов по id строк
            index.Root.SortByKey<long>(ob => (long)((object[])ob)[1]);
        }
        
        public Dictionary<string, long> InsertPortion(string[] sortedArray)
        {
            Dictionary<string, long> dictionary = new Dictionary<string, long>();
            if (sortedArray.Length == 0) 
                return dictionary;
            bool portionIsOver = false;

            int indexName = 0;
            string nameFromPortion = sortedArray[indexName];

            long newCode = (int)tableNames.Root.Count();

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
                        object[] newPair = new object[] { newCode, nameFromPortion };
                        temp.Root.AppendElement(newPair);
                        newCode++;
                        dictionary.Add((string)newPair[1], (long)newPair[0]);
                    }
                    else
                    {
                        dictionary.Add((string)pair[1], (long)pair[0]);
                    }
                    indexName++;
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
                object[] newPair = new object[] { newCode, nameFromPortion };
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
            return dictionary;
        }

    }
}
