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
        public PaCell tableNames, offsets, index; //TODO: private

        private long GetIdByString(string srchStr) 
        { 
            return -1; 
        }

        public string GetStringById(int id)
        {
            if (id < 0 || id > tableNames.Root.Count() || offsets.IsEmpty)
                throw new Exception("Строки с таким id нет");

            if (tableNames.Root.Count()==0 || tableNames.IsEmpty)
                throw new Exception("Таблица имен пуста");

            PaEntry ent = tableNames.Root.Element(0);
            ent.offset = (long)offsets.Root.Element(id).Get();
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
                new NamedType("id", new PType(PTypeEnumeration.integer)))
            );

            offsets = new PaCell(tp_of, path + "offsets.pac", false);
            index = new PaCell(tp_id, path + "index.pac", false);
        }

        public void CreateIndex()
        {
            if (tableNames.Root.Count() == 0) return;

            index.Clear(); 
            index.Fill(new object[0]);

            foreach(PaEntry ent in tableNames.Root.Elements())
            {
                index.Root.AppendElement(new object[] { ent.offset, ent.Field(0).Get() });
            }
            index.Flush();

            //Сортировка офсетов по id строк
            index.Root.SortByKey<int>(ob => (int)((object[])ob)[1]);
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
                        nameFromPortion = sortedArray[indexName];
                        // используем новый код
                        object[] newPair = new object[] { newCode, nameFromPortion };
                        temp.Root.AppendElement(newPair);
                        newCode++;
                        dictionary.Add((string)newPair[1], (long)newPair[0]);
                    }
                    indexName++;
                    if (indexName < sortedArray.Length)
                        nameFromPortion = sortedArray[indexName];
                    else
                        portionIsOver = true;
                }
                temp.Root.AppendElement(pair); // переписывается тот же объект
                dictionary.Add((string)pair[1], (long)pair[0]);
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
            try
            {
                System.IO.File.Delete(path + "TableOfNames.pac");
                System.IO.File.Move(path + "temp.pac", path + "TableOfNames.pac");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            tableNames = new PaCell(tp, path + "TableOfNames.pac", false);

            return dictionary;
        }

    }
}
