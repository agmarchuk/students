using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableOfNamesSQL
{
    /// <summary>
    /// Testing SQL Databases
    /// </summary>
    class Testing
    {
        //путь до базы
        private const string path = "../../../Databases/";
        //путь до файла результатов теста
        private const string ResultsPath = "../../../TableOfNamesSQL/ResultRunSQL.txt";

        public static void Run(ISQLDB db, int NumberData)
        {
            Random rnd = new Random();

            db.PrepareToLoad();

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            Console.WriteLine("\n" + db.ToString());

            sw.Start();
            db.LoadElementFlow(NumberData);
            sw.Stop();
            Console.WriteLine("Добавление данных. Время={0}", sw.ElapsedMilliseconds);

            sw.Restart();
            db.MakeIndexes();
            sw.Stop();
            Console.WriteLine("Создание индексов. Время={0}", sw.ElapsedMilliseconds);

            sw.Reset();
            for (int i = 0; i < 1000; ++i)
            {
                sw.Start();
                db.SelectByIdFirst(rnd.Next(2 * NumberData), "TestStrings");
                sw.Stop();
            }
            Console.WriteLine("Поиск 1000 раз строки по id. Время={0}", sw.ElapsedMilliseconds);

            //sw.Reset();
            //for (int i = 0; i < 1000; ++i)
            //{
            //    string s = "s" + rnd.Next(2 * NumberData);
            //    sw.Start();
            //    db.SearchByNameFirst(s, "TestStrings");
            //    sw.Stop();
            //}
            //Console.WriteLine("Поиск 1000 раз id по строке. Время={0}", sw.ElapsedMilliseconds);
        }

        static void Main(string[] args)
        {

            SQLite sqlite = null;
            //MySQL  mysql  = null;
            MSSQL mssql = null;
            TextWriter standardOutput = Console.Out;
            StreamWriter outf = null;
            try
            {
                outf = new StreamWriter(ResultsPath);
                Console.WriteLine("Запуск теста для SQL DB. Результат пишется в файл {0}",
                                    Path.GetFileNameWithoutExtension(ResultsPath));

                Console.SetOut(outf);

                int N = 10;
                Console.WriteLine("Кол-во данных: {0}", N);


                sqlite = new SQLite(@"Data Source=" + path + @"sqlite.db3; 
                            New=True; 
                            UseUTF16Encoding=True", 
                            path);
    //            MySQL  mysql  = new MySQL(@"server=localhost;
    //                                        uid=root;
    //                                        pwd=1234;",
    //                                        "TestMySQL");

                mssql  = new MSSQL(@"Data Source=(LocalDB)\v11.0;"+
                                            "Integrated Security=True;"+ 
                                            "Connect Timeout=30",
                                            Environment.CurrentDirectory+"/",
                                            "mssql");
                Testing.Run(sqlite, N);
                sqlite.Dispose();
                //Testing.Run(mysql, N);
                //mysql.Dispose();
                Testing.Run(mssql, N);
                mssql.Dispose();

            }
            catch (Exception ex)
            {
                standardOutput.WriteLine(ex.ToString());
                standardOutput.WriteLine("Press any key...");
                Console.ReadKey();
            }
            finally
            {
                Console.SetOut(standardOutput);
                if (outf != null)
                    outf.Dispose();
            }
        }

    }


}