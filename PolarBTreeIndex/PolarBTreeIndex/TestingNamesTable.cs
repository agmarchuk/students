using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolarBtreeIndex
{
    class TestingNamesTable
    {
        //путь до базы
        private const string path = @"../../../Databases/";
        //путь до файла результатов теста
        private const string ResultsPath = "../../../../PolarBTreeIndex/Results/";

        private static const int dataSize = 1000;
        private static TextWriter standardOutput = Console.Out;
        private static StreamWriter outf = new StreamWriter(string.Format(ResultsPath + "ResultTest1.txt"));

        static void Main(string[] args)
        {
            try
            {
                Console.SetOut(outf);
                standardOutput.WriteLine("Запуск теста №1");
                //------------
                Test1();
                //Test2();
                //Test3();
                //------------
                standardOutput.WriteLine("Теста №1 завершен. Результат в файле {0}",
                                            Path.GetFileNameWithoutExtension(ResultsPath));
            }
            catch (Exception ex) { standardOutput.WriteLine(ex.Message); }
            finally
            {
                if (outf != null)
                    outf.Dispose();

                standardOutput.WriteLine("Для продолжения нажмите любую клавишу...");
                Console.ReadKey();
            }
        }

        private static void Test1()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random();
            NamesTable nt = new NamesTable(path);

            standardOutput.WriteLine("Начался процесс добавления рандомных данных...");
            sw.Start();
                nt.LoadTable((uint)dataSize, 1);
            sw.Stop();
            standardOutput.WriteLine("Добавление данных завершено. Всего данных = {0}, время = {1}", dataSize, sw.ElapsedMilliseconds);
            Console.WriteLine("Добавление данных. Всего данных = {0}, время = {1}",dataSize, sw.ElapsedMilliseconds);

            standardOutput.WriteLine("\nПостроение индексов...");
            sw.Restart();
                nt.MakeIndex();
            sw.Stop();
            standardOutput.WriteLine("Индексы построены. Время = {0}", sw.ElapsedMilliseconds);
            Console.WriteLine("Построение индексов. Время = {0}", sw.ElapsedMilliseconds);

            standardOutput.WriteLine("\nРазогрев...");
            sw.Restart();
                nt.Warmup();
            sw.Stop();
            standardOutput.WriteLine("Разогрев завершен. Время = {0}", sw.ElapsedMilliseconds);
            Console.WriteLine("Разогрев. Время={0}", sw.ElapsedMilliseconds);

            standardOutput.WriteLine("\nПоиск строки 1000 раз...");
            for (int i = 0; i < 1000; ++i)
            {
                int id = rnd.Next(dataSize);
                sw.Start();
                    nt.GetStringById(id);
                sw.Stop();
                //standardOutput.WriteLine("Искомая строка: {0}", nt.GetStringById(id));
            }
            standardOutput.WriteLine("Поиск строки по id. Время = {0}", sw.ElapsedMilliseconds);
            Console.WriteLine("Поиск строки по id. Время = {0}", sw.ElapsedMilliseconds);

            sw.Reset();
            standardOutput.WriteLine("\nПоиск id 1000 раз...");
            for (int i = 0; i < 1000; ++i)
            {
                string s = "s" + rnd.Next(100000000);
                sw.Start();
                    nt.GetIdByString(s);
                sw.Stop();
                //standardOutput.WriteLine("Искомый id строки {0} равен = {1}", s, nt.GetIdByString(s));
            }
            standardOutput.WriteLine("Поиск id по строке. Время = {0}", sw.ElapsedMilliseconds);
            Console.WriteLine("Поиск id по строке. Время = {0}", sw.ElapsedMilliseconds);

            nt.Dispose();
            nt.Delete();
        }

        private static void Test2()
        {
            NamesTable nt = new NamesTable(path);
            nt.LoadTable(10, 1);
            nt.MakeIndex();

            standardOutput.WriteLine("Вывод дерева в файл...");
            nt.WriteTreeInFile(ResultsPath+"BTree.txt");
            standardOutput.WriteLine("Вывод дерева в файл закончен. Файл находится в " + Path.GetDirectoryName(ResultsPath + "BTree.txt"));
            
            nt.Dispose();
            nt.Delete();
        }

        private static void Test3()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            NamesTable nt = new NamesTable(path);
            nt.LoadTable((uint)dataSize, 1);
            nt.MakeIndex();

            nt.Add("TryFindme");

            sw.Restart();
            standardOutput.WriteLine("\nПоиск строки...");
            if (nt.GetIdByString("TryFindme") != null) Console.WriteLine("Нашлась");
            else
                Console.WriteLine("Не нашлась");
            sw.Stop();
            standardOutput.WriteLine("Время={0}", sw.ElapsedMilliseconds);


            //nt.Remove("TryFindme");

            //sw.Restart();
            //standardOutput.WriteLine("\nПоиск удаленной строки...");
            //if (nt.GetIdByString("TryFindme") != null) standardOutput.WriteLine("Нашлась");
            //else
            //    standardOutput.WriteLine("Не нашлась");
            //sw.Stop();
            //standardOutput.WriteLine("Время={0}", sw.ElapsedMilliseconds);

            nt.Dispose();
            nt.Delete();
        }
        
    }
}
