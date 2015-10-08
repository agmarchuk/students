using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarBtreeIndex;

namespace TestPlatform.SpeedTests
{
    public class TestTableOfNamesBTreePolar
    {
        //путь до базы
        private const string path = @"../../../../Databases/";
        //путь до файла результатов теста
        private const string resultsPath = "../../../PolarBTreeIndex/Results/";
        private static string fileResult = null;

        //файл с выводом дерева
        private const string BTreePath = resultsPath + "BTree.txt";

        private const int dataSize = 10000;

        private static TextWriter standardOutput = Console.Out;
        private static StreamWriter outf = null;

        public static void Main1(string[] args)
        {
            try
            {
                int numTest = 1;
                int counter = 9;
                //for (int numTest = 1; numTest <= 2; ++numTest)
                {
                    fileResult = String.Format(resultsPath + "ResultTest1_{0}.txt", counter);

                    if (!File.Exists(fileResult))
                        using (File.CreateText(fileResult)) { };

                    outf = new StreamWriter(fileResult);

                    Console.SetOut(outf);

                    standardOutput.WriteLine("Запуск теста №{0}",numTest);
                    standardOutput.WriteLine("-------------------------------------------------------------------------------");
                    //------------
                    switch (numTest)
                    {
                        case 1: { Test1(); break; }
                        //case 2: { Test2(); break; }
                        //case 3: { Test3(); break; }
                    }
                    //------------
                    standardOutput.WriteLine("-------------------------------------------------------------------------------");
                    standardOutput.WriteLine("Теста №{0} завершен.",numTest);
                }
            }
            catch (Exception ex) { standardOutput.WriteLine("Ошибка: "+ex.Message); }
            finally
            {
                if (outf != null)
                    outf.Dispose();

                Console.SetOut(standardOutput);
                Console.WriteLine("\nДля продолжения нажмите любую клавишу...");
                Console.ReadKey();
            }
        }

        private static void Test1(bool warmup = true)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random();
            NamesTable nt = new NamesTable(path);

            standardOutput.WriteLine("Начался процесс добавления данных...");
            sw.Start();
                nt.LoadTable((uint)dataSize, 1);
            sw.Stop();
            standardOutput.WriteLine("Добавление данных завершено. Всего данных = {0}, время = {1}мс", nt.GetCount(), sw.ElapsedMilliseconds);
            Console.WriteLine("Добавление данных. Всего данных = {0}, время = {1}мс", nt.GetCount(), sw.ElapsedMilliseconds);

            standardOutput.WriteLine("\nПостроение индексов...");
            sw.Restart();
                nt.MakeIndex();
            sw.Stop();
            standardOutput.WriteLine("Индексы построены. Время = {0}мс", sw.ElapsedMilliseconds);
            Console.WriteLine("Построение индексов. Время = {0}мс", sw.ElapsedMilliseconds);

            if (warmup)
            {
                standardOutput.WriteLine("\nРазогрев...");
                sw.Restart();
                    nt.Warmup();
                sw.Stop();
                standardOutput.WriteLine("Разогрев завершен. Время = {0}мс", sw.ElapsedMilliseconds);
                Console.WriteLine("Разогрев. Время={0}мс", sw.ElapsedMilliseconds);
            }
            
            sw.Reset();
            standardOutput.WriteLine("\nПоиск строки 1000 раз...");
            for (int i = 0; i < 1000; ++i)
            {
                int id = rnd.Next(dataSize);
                sw.Start();
                    nt.GetStringById(id);
                sw.Stop();
                //standardOutput.WriteLine("Искомая строка: {0}", nt.GetStringById(id));
            }
            standardOutput.WriteLine("Поиск строки по id. Время = {0}мс", sw.ElapsedMilliseconds);
            Console.WriteLine("Поиск строки по id. Время = {0}мс", sw.ElapsedMilliseconds);

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
            standardOutput.WriteLine("Поиск id по строке. Время = {0}мс", sw.ElapsedMilliseconds);
            Console.WriteLine("Поиск id по строке. Время = {0}мс", sw.ElapsedMilliseconds);

            Console.WriteLine("Степень дерева = {0}", nt.GetBtreeDegree());

            nt.Dispose();
            nt.Delete();
            
            standardOutput.WriteLine("\nРезультат сохранен в файле {0}", Path.GetFileName(fileResult));
        }

        private static void Test2()
        {
            NamesTable nt = new NamesTable(path);
            nt.LoadTable(10, 1);
            nt.MakeIndex();

            standardOutput.WriteLine("Вывод дерева в файл...");
            nt.WriteTreeInFile(BTreePath);
            standardOutput.WriteLine("Вывод дерева в файл закончен. Файл находится в " + resultsPath + "BTree.txt");

            standardOutput.WriteLine("+++++++++++|Таблица имен|+++++++++++++");
            nt.ShowBearingTable(standardOutput);
            standardOutput.WriteLine("++++++++++++++++++++++++++++++++++++++");

            nt.Dispose();
            nt.Delete();
        }

        private static void Test3()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            NamesTable nt = new NamesTable(path);
            nt.LoadTable((uint)dataSize, 1);

            nt.Add("TryFindme");
            nt.MakeIndex();

            standardOutput.WriteLine("Вывод дерева в файл...");
            nt.WriteTreeInFile(BTreePath);
            standardOutput.WriteLine("Вывод дерева в файл закончен. Файл находится в " + resultsPath + "BTree.txt");

            standardOutput.WriteLine("+++++++++++|Таблица имен|+++++++++++++");
            nt.ShowBearingTable(standardOutput);
            standardOutput.WriteLine("++++++++++++++++++++++++++++++++++++++");

            sw.Restart();
            standardOutput.WriteLine("\nПоиск строки...");
            if (nt.GetIdByString("TryFindme") != null) standardOutput.WriteLine("Нашлась");
            else
                standardOutput.WriteLine("Не нашлась");
            sw.Stop();
            standardOutput.WriteLine("Время={0}мс", sw.ElapsedMilliseconds);


            //nt.Remove("TryFindme");

            //sw.Restart();
            //standardOutput.WriteLine("\nПоиск удаленной строки...");
            //if (nt.GetIdByString("TryFindme") != null) standardOutput.WriteLine("Нашлась");
            //else
            //    standardOutput.WriteLine("Не нашлась");
            //sw.Stop();
            //standardOutput.WriteLine("Время={0}мс", sw.ElapsedMilliseconds);

            nt.Dispose();
            nt.Delete();
        }
     
    }
}
