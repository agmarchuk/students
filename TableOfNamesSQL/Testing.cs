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
            using (SQLDB<ISQLDB> sqldb = new SQLDB<ISQLDB>(db))
            {
                Random rnd = new Random();

                try
                {
                    sqldb.InitSQLDB();
                }
                catch (Exception)
                {
                    throw;
                }

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                Console.WriteLine("\n" + db.ToString());

                sw.Start();
                sqldb.LoadSQL(NumberData);
                sw.Stop();
                Console.WriteLine("Добавление данных. Время={0}", sw.ElapsedMilliseconds);

                sw.Restart();
                sqldb.CreateIndexSQL();
                sw.Stop();
                Console.WriteLine("Создание индексов. Время={0}", sw.ElapsedMilliseconds);

                sw.Reset();
                for (int i = 0; i < 1000; ++i)
                {
                    sw.Start();
                    sqldb.SearchByIDSQLFirst(rnd.Next(2 * NumberData));
                    sw.Stop();
                }
                Console.WriteLine("Поиск 1000 раз строки по id. Время={0}", sw.ElapsedMilliseconds);

                sw.Reset();
                for (int i = 0; i < 1000; ++i)
                {
                    string s = "s" + rnd.Next(2 * NumberData);
                    sw.Start();
                    sqldb.SearchByStringSQLFirst(s);
                    sw.Stop();
                }
                Console.WriteLine("Поиск 1000 раз id по строке. Время={0}", sw.ElapsedMilliseconds);

                sqldb.DeleteSQLDB();
            }
        }

        static void Main(string[] args)
        {
            SQLite sqlite = new SQLite(@"Data Source=" + path + @"sqlite.db3; 
                                        New=True; 
                                        UseUTF16Encoding=True", 
                                        path);
            MySQL  mysql  = new MySQL(@"server=localhost;
                                        uid=root;
                                        pwd=1234;",
                                        "TestMySQL");

            MSSQL mssql  = new MSSQL(@"Data Source=(LocalDB)\v11.0;"+
                                        "Integrated Security=True;"+ 
                                        "Connect Timeout=30",
                                        Environment.CurrentDirectory+"/",
                                        "mssql");

            TextWriter tmp = Console.Out;
            try
            {
                using (StreamWriter outf = new StreamWriter(ResultsPath))
                {
                    Console.WriteLine("Запуск теста для SQL DB. Результат пишется в файл {0}", 
                                        Path.GetFileNameWithoutExtension(ResultsPath));
                    
                    if (outf != null) Console.SetOut(outf);

                    int N = 1000000;
                    Console.WriteLine("Кол-во данных: {0}", N);

                    try
                    {
                        Testing.Run(sqlite, N);
                        Testing.Run(mysql, N);
                        Testing.Run(mssql, N);
                    }
                    catch (Exception) 
                    { 
                        throw;
                    }
                    finally
                    {
                        Console.SetOut(tmp);
                    }
                }
                
            }
            catch (Exception ex) { 
                Console.WriteLine(ex.Message); 
                Console.WriteLine("Press any key..."); 
                Console.ReadKey(); }
        }

    }


}