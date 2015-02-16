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
        private PaCell tableNames, offsets, index;

        private long GetIdByString(string srchStr);

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

        public Dictionary<string, int> InsertPortion(string[] sorted_arr)
        {
            
            Dictionary<string, int> dic = new Dictionary<string, int>();

            return dic;

        }



    }
}
