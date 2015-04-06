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

        //private void TestGenerator()
        //{
        //    //Проверка генерации строк
        //    TestDataGenerator tdg = new TestDataGenerator(10);

        //    foreach( string str in tdg.Generate() )
        //    {
        //        Console.WriteLine(str);
        //    }
        //}

        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TableOfNames ton = new TableOfNames(path);
            Random rnd = new Random();

            uint portion = 10;
            uint countPortions = 1;

            Console.WriteLine("Начался процесс добавления рандомных данных...");
            for (uint i = 0; i < countPortions; i++)
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
                ton.InsertPortion(arr);
                sw.Stop();
            }

            //foreach (object[] pair in ton.tableNames.Root.ElementValues())
            //{
            //    Вывод таблицы имен
            //    Console.WriteLine((long)pair[0] + " " + (string)pair[1]);
            //}
            Console.WriteLine("Загрузка закончена. Время={0}", sw.ElapsedMilliseconds);

            sw.Restart();
            Console.WriteLine("\nПостроение индексов...");
            //ton.SlowCreateIndex();
            ton.CreateIndex();
            sw.Stop();
            Console.WriteLine("Индексы построены. Время={0}", sw.ElapsedMilliseconds);
            Console.ReadKey();

            //Console.WriteLine();
            //ton.TreeShow();
            //Console.WriteLine();

            sw.Reset();
            int N = (int)ton.GetCount();

            Console.WriteLine("\nПоиск строки 1000 раз...");
            for (int i = 0; i < 1000; ++i)
            {
                int id = rnd.Next(N);
                sw.Start();
                ton.GetStringById(id);
                sw.Stop();
                //Console.WriteLine("Искомая строка: {0}", ton.GetStringById(id));
            }
            Console.WriteLine("Поиск строки по id. Время={0}", sw.ElapsedMilliseconds);

            sw.Reset();
            Console.WriteLine("\nПоиск id 1000 раз...");
            for (int i = 0; i < 1000; ++i)
            {
                string s = "s" + rnd.Next(100000000);
                sw.Start();
                ton.GetIdByString(s);
                sw.Stop();
                //Console.WriteLine("Искомый id строки {0} равен = {1}", s, ton.GetIdByString(s));
            }
            Console.WriteLine("Поиск id по строке. Время={0}", sw.ElapsedMilliseconds);

            Console.ReadKey();
        }
    }
}
