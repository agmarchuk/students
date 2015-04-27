using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Net;

namespace TableOfNames
{
    class TestingTableOfNames
    {
        //путь до базы
        private const string path = "../../../Databases/";

        public static void Run1()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TableOfNames ton = new TableOfNames(path);
            Random rnd = new Random();

            Console.WriteLine("Начался процесс добавления рандомных данных...");
            ton.LoadTable(10, 1);

            //foreach (object[] pair in ton.tableNames.Root.ElementValues())
            //{
            //    Вывод таблицы имен
            //    Console.WriteLine((long)pair[0] + " " + (string)pair[1]);
            //}

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

            ton.Warmup();

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

            ton.Dispose();
        }

        public static void Run2()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TableOfNames ton = new TableOfNames(path);
            Random rnd = new Random();

            Console.WriteLine("Начался процесс добавления рандомных данных...");
            ton.LoadTable(10000, 1);

            sw.Restart();
            Console.WriteLine("\nПостроение индексов...");
            ton.SlowCreateIndex();
            sw.Stop();
            Console.WriteLine("Индексы построены. Время={0}", sw.ElapsedMilliseconds);

            ton.Add("TryFindme");

            ton.ShowSizeDictionary();
            Console.ReadKey();

            sw.Restart();
            Console.WriteLine("\nПоиск строки...");
            if (ton.GetIdByString("TryFindme") != null) Console.WriteLine("Нашлась");
            else 
                Console.WriteLine("Не нашлась");
            sw.Stop();
            Console.WriteLine("Время={0}", sw.ElapsedMilliseconds);


            ton.Remove("TryFindme");

            ton.ShowSizeDictionary();
            Console.ReadKey();

            sw.Restart();
            Console.WriteLine("\nПоиск удаленной строки...");
            if (ton.GetIdByString("TryFindme") != null) Console.WriteLine("Нашлась");
            else
                Console.WriteLine("Не нашлась");
            sw.Stop();
            Console.WriteLine("Время={0}", sw.ElapsedMilliseconds);

            ton.Dispose();
        }

        public static void Run3()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TableOfNames ton = new TableOfNames(path);

            sw.Start();
            for (int i = 0; i < 100000; ++i)
            {
                ton.Add("TryFindme"+i);
            }
            sw.Stop();
            Console.WriteLine("Добавление данных и создание индекса. Время={0}", sw.ElapsedMilliseconds);
            
            //sw.Restart();
            //Console.WriteLine("\nПоиск строки...");
            //if (ton.GetIdByString("TryFindme999") != null) Console.WriteLine("Нашлась");
            //else
            //    Console.WriteLine("Не нашлась");
            //sw.Stop();
            //Console.WriteLine("Время={0}", sw.ElapsedMilliseconds);

            Console.WriteLine("\nНачался процесс слияния индексов...");
            sw.Start();
            ton.Dispose();
            sw.Stop();
            Console.WriteLine("Время слияния={0}", sw.ElapsedMilliseconds);
        }

        private static void Run4()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TableOfNames ton = new TableOfNames(path);
            Random rnd = new Random();

            Console.WriteLine("Начался процесс добавления рандомных данных...");
            ton.LoadTable(1000000, 1);

            Console.WriteLine("Добавление данных. Всего данных = {0}", ton.GetCount());

        }

        private static void RunCacheSearch()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TableOfNames ton = new TableOfNames(path);

            sw.Restart();
            Console.WriteLine("\nПоиск строки...");
            if (ton.GetIdByString("TryFindme999") != null)
            {
                //Если нашлось значение, помещаем строку в кеш
                //ton.CacheIndex("TryFindme999");
                Console.WriteLine("Нашлась");
            }
            else
                Console.WriteLine("Не нашлась");

            sw.Stop();
            Console.WriteLine("Время={0}", sw.ElapsedMilliseconds);


            Console.WriteLine("\nНачался процесс слияния индексов...");
            sw.Start();
            ton.Dispose();
            sw.Stop();
            Console.WriteLine("Время слияния={0}", sw.ElapsedMilliseconds);
        }

        static void Main(string[] args)
        {
            try
            {
                //Run1();
                //Run2();
                //Run3();
                Run4();
                //RunCacheSearch();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
            finally
            {
                Console.ReadKey();
                System.IO.File.Delete(path + "index.pac");
                System.IO.File.Delete(path + "IndexNames.pax");
                System.IO.File.Delete(path + "TableOfNames.pac");
                
            }
        }
    }
}
