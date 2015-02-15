using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableOfNames
{
    class TestingTableOfNames
    {
        //путь до базы
        private const string path = "../../../Databases/";

        private void TestGenerator()
        {
            //Проверка генерации строк
            TestDataGenerator tdg = new TestDataGenerator(10);

            foreach( string str in tdg.Generate() )
            {
                Console.WriteLine(str);
            }
        }

        private static string GetStringById(PaCell baseTable,PaCell indexTable, long id)
        {
            return (string)baseTable.Root.Element(id).Get();
        }

        private static PaEntry TestSearch(PaCell baseTable, BinaryTreeIndex cell, object ob)
        {
            PaEntry entry = baseTable.Root.Element(0);
            bool Founded = false;

            PxEntry found = cell.BinarySearch(pe =>
            {
                entry.offset = (long)pe.Get();

                object get = entry.Get();

                int rezcmp = cell.elementCompare(ob, get);
                if (rezcmp == 0) Founded = true;

                return rezcmp;
            });
            if (Founded == true) entry.offset = (long)found.Get();
            else entry.offset = 0;

            return entry;
        }

        private static long GetIdByString(PaCell baseTable, BinaryTreeIndex indexTable, string srchStr)
        {
            PaEntry entt = baseTable.Root.Element(0);
            long g = baseTable.Root.Element(0).offset;

            entt = TestSearch(baseTable, indexTable, srchStr);//вернется офсет на первый найденный элемент в БД

            //TODO: исправить костыль
            if (entt.offset != 0) return (long)((entt.offset - baseTable.Root.Element(0).offset) / 8 - 1);
            else return 0L;
        }
        static void Main(string[] args)
        {
            //задаём тип для записи в ячейку БД
            PType tp = new PTypeSequence(new PType(PTypeEnumeration.sstring));
            
            //создаём БД
            PaCell cell = new PaCell(tp, path + "test.pac", false);

            //очистка БД
            cell.Clear();
            cell.Fill(new object[0]);
            
            TestDataGenerator tdg = new TestDataGenerator(10);

            //записываем сгенерированные данные
            foreach (string str in tdg.Generate())
            {
                cell.Root.AppendElement(str);
                Console.WriteLine(str);
            }
            cell.Flush();

            //тестовый вывод БД
            //Console.WriteLine(tp.Interpret(cell.Root.Get()));

            //функция сравнения строк
            Func<object, object, int> compare_string = (object ob1, object ob2) =>
            {
                return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
            };

            PType tp_btr = new PType(PTypeEnumeration.longinteger);
            PType tp_id = new PTypeSequence(new PType(PTypeEnumeration.longinteger));

            //создание индексов в виде бинарного дерева
            BinaryTreeIndex index_str = new BinaryTreeIndex(tp_btr, cell, 0,
                compare_string, path + "index_str.pxc", readOnly: false);
            //создание индексов в виде последовательности оффсетов
            PaCell index_id = new PaCell(tp_id, path + "index_id.pac", false);

            index_str.Clear();
            index_id.Clear();
            index_id.Fill(new object[0]);

            //заполняем индексные файлы офссетами
            foreach (PaEntry ent in cell.Root.Elements())
            {
                long offset = ent.offset;
                index_str.Add(offset);
                index_id.Root.AppendElement(offset);
            }

            index_id.Flush();

            //Вывод индексов на терминал
            var res = index_str.Root.GetValue();
            Console.WriteLine(res.Type.Interpret(res.Value));

            Console.WriteLine();
            var res2 = index_id.Root.GetValue();
            Console.WriteLine(res2.Type.Interpret(res2.Value));


            Console.WriteLine("Искомая строка: {0}", GetStringById(cell, index_id, 8));
            Console.WriteLine(
                "Искомый индекс строки {1}: {0}", 
                GetIdByString(cell, index_str, GetStringById(cell, index_id, 8)), 
                GetStringById(cell, index_id, 8)
                );

            Console.ReadKey();
        }
    }
}
