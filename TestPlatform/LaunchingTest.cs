using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestPlatform.SpeedTests;

namespace TestPlatform
{
    class LaunchingTest
    {
        private static TextWriter standardOutput = Console.Out;
        private static StreamWriter outf = null;
        private static string fileResult = null;
        private const string resultsPath = @"..\..\..\..\students\TestPlatform\ResultTests\";

        public static void RunTest(IPerformanceTest test, string name)
        {
            //int N = 1000000;
            try
            {
                test.Init();
                Console.WriteLine("Тестирование {0}", name);
                int step = 100;
                for (int i = 1; i <= 1; ++i)
                {
                    Console.WriteLine("{1} раз добавляем в БД {2} записей. Время шага = {0}мс", test.Add(step), i, step);
                    //int repeats = 1000;
                    //Console.WriteLine("Поиск первого строкового ключа {0} раз. Время = {1}мс", repeats, test.FindString(repeats));
                    //Console.WriteLine("Поиск первого целого ключа {0} раз. Время = {1}мс", repeats, test.FindInt(repeats));
                    
                }
            }
            catch (Exception ex) { standardOutput.WriteLine("Ошибка: " + ex); }
            finally { test.DeleteDB(); }
        }

        public static void Main(string[] arg)
        {
            //var polarDB = new TestPolarDB();
            //var ORMpolarDB = new TestORMPolarDB();
            //var ORMEntityFramework = new TestORMEntityFramework();
            var ORMNHibernate = new TestORMNHibernate();

            ArrayList tests = new ArrayList();
            //tests.Add(polarDB);
            //tests.Add(ORMpolarDB);
            tests.Add(ORMNHibernate);//Требует существование БД BookStore на сервере и 
            //очистку после теста от созданных таблиц
            //tests.Add(ORMEntityFramework);//DBConnection берет из параметров проекта

            //tests.Add(HashTableIndexPolarDB);
            //tests.Add(exHashTableIndexPolarDB);

            string time = String.Format("{0}_{1}-{2} {3}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Hour, DateTime.Now.Minute);
            fileResult = resultsPath + "ResultTests_"+time+".txt";

            try
            {
                if (!File.Exists(fileResult))
                    using (File.CreateText(fileResult)) { };

                outf = new StreamWriter(fileResult);

                Console.SetOut(outf);

                standardOutput.WriteLine("Началось тестирование");

                foreach (IPerformanceTest test in tests)
                {
                    standardOutput.WriteLine(test.GetType().Name);
                    RunTest(test, test.GetType().Name);
                    Console.WriteLine();
                }
                
            }
            catch (Exception ex) { standardOutput.WriteLine("Ошибка: " + ex.Message); }
            finally
            {
                if (outf != null)
                    outf.Dispose();

                Console.SetOut(standardOutput);
                Console.WriteLine("Процесс тестирования окончен");
            }

            Console.ReadLine();
        }

    }
}
