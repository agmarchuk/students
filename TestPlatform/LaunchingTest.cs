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
        private const string resultsPath = @"..\..\..\..\..\students\TestPlatform\ResultTests\";

        public static void RunTest(IPerformanceTest test, string name)
        {
            int N = 10;
            try
            {
                Console.WriteLine("Тестирование {0}", name);
                Console.WriteLine("Создание БД c {1} записями. Время = {0}мс", test.CreateDB(N), N);

                Console.WriteLine("Разогрев. Время = {0}мс", test.WarmUp());

                Console.WriteLine("Поиск первого строкового ключа. Время = {0}мс", test.FindFirst(1, "title"));
                Console.WriteLine("Поиск первого целого ключа. Время = {0}мс", test.FindFirst(1, "id_author"));

                Console.WriteLine("Поиск всех целых ключей. Время = {0}мс", test.FindAll(1, "id_author"));
                Console.WriteLine("Поиск всех строковых ключей. Время = {0}мс", test.FindAll(1, "title"));

                int repeats = 1000;
                Console.WriteLine("Поиск первого строкового ключа {0} раз. Время = {1}мс", repeats, test.FindFirst(repeats, "title"));
                Console.WriteLine("Поиск первого целого ключа {0} раз. Время = {1}мс", repeats, test.FindFirst(repeats, "id_author"));
                Console.WriteLine("Поиск всех целых ключей {0} раз. Время = {1}мс", repeats, test.FindAll(repeats, "id_author"));
                Console.WriteLine("Поиск всех строковых ключей {0} раз. Время = {1}мс", repeats, test.FindAll(repeats, "title"));

                test.DeleteDB();
            }
            catch (Exception ex) { standardOutput.WriteLine("Ошибка: " + ex); }
        }

        public static void Main(string[] arg)
        {
            var polarDB = new TestPolarDB();
            var ORMpolarDB = new TestORMPolarDB();
            var DBInRAM = new TestDBInRAM();
            var ORMEntityFramework = new TestORMEntityFramework();
            var ORMNHibernate = new TestORMNHibernate();

            ArrayList tests = new ArrayList();
            tests.Add(polarDB);
            tests.Add(ORMpolarDB);
            tests.Add(DBInRAM);
            tests.Add(ORMNHibernate);//Требует существование БД BookStore на сервере и 
                                     //очистку после теста от созданных таблиц
            tests.Add(ORMEntityFramework);//DBConnection берет из параметров проекта

            string time = String.Format("{0}_{1}-{2}", DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute);
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
