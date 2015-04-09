using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Xml.Linq;

namespace TableOfNamesSQL
{
    public class SQLite : ISQLDB
    {
        private SQLiteConnection connection = null;
        private string path = "";

        public SQLite(string connectionString, string path)
        {
            this.path = path;
            SQLiteConnection.CreateFile(path + "sqlite.db3");
            connection = new SQLiteConnection(connectionString + ";Version=3;");
        }

        public void Delete()
        {
            //удаление БД только через удаление файла
            System.IO.File.Delete(path + "sqlite.db3");
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public void PrepareToLoad()
        {
            using(DbCommand comm = connection.CreateCommand())
            {
                connection.Open();
                comm.CommandText =
                @"CREATE TABLE TestStrings (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
                                            name NVARCHAR(255), 
                                            deleted INTEGER DEFAULT 0);";

                comm.ExecuteNonQuery();
                connection.Close(); 
            }
        }

        public void MakeIndexes()
        {
            connection.Open();

            using (DbCommand comm = connection.CreateCommand())
            {
                //comm.CommandTimeout = 2000;
                comm.CommandText = "CREATE INDEX index_name ON TestStrings(name);";
                try
                {
                    comm.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void LoadElementFlow(int NumberData)
        {
            connection.Open();

            using (DbCommand runcomm = RunStart())
            {
                Random rnd = new Random();

                for (int i = 0; i < NumberData; ++i)
                {
                    string names = "s" + rnd.Next(2 * NumberData); ;
                    runcomm.CommandText = "INSERT INTO TestStrings(name) VALUES ('" + names + "');";
                    runcomm.ExecuteNonQuery();
                }

                RunStop(runcomm);
            }
        }

        public void Count(string table)
        {
            connection.Open();

            using (var comm = connection.CreateCommand())
            {
                //comm.CommandTimeout = 1000;
                comm.CommandText = "SELECT COUNT(*) FROM " + table + ";";
                var obj = comm.ExecuteScalar();
                Console.WriteLine("Кол-во записей = {0}", obj);
            }
            connection.Close();
        }
        public void SelectByIdAll(int id, string table)
        {
            connection.Open();

            using (var comm = connection.CreateCommand())
            {
                //comm.CommandTimeout = 1000;
                comm.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE id=" + id + ";";
                //var reader = comm.ExecuteReader();
                //Console.WriteLine("id={0} name={1} deleted={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
                var obj = comm.ExecuteScalar();
                Console.WriteLine("Кол-во найденных: {0}", obj);
            }
            connection.Close();
        }

        public bool SelectByIdFirst(int id, string table)
        {
            connection.Open();
            string name = "";
            using (var comm = connection.CreateCommand())
            {
                comm.CommandText = "SELECT name FROM " + table + " WHERE id=" + id + ";";
                using (var reader = comm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        name = reader.GetString(0);
                    }
                }
            }
            connection.Close();
            return name != "" ? true : false;
        }

        public void SearchByNameAll(string searchstring, string table)
        {
            connection.Open();

            using (var comm = connection.CreateCommand())
            {
                comm.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE name LIKE '" + searchstring + "%'";
                //var reader = comm.ExecuteReader();
                //Console.WriteLine("id={0} name={1} deleted={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
                var obj = comm.ExecuteScalar();
                Console.WriteLine("Кол-во найденных: {0}", obj);
            }
            connection.Close();
        }

        public bool SearchByNameFirst(string searchstring, string table)
        {
            connection.Open();
            int id = 0;
            using (var comm = connection.CreateCommand())
            {
                comm.CommandText = "SELECT id FROM " + table + " WHERE name LIKE '" + searchstring + "%' LIMIT 1,1";

                using (var reader = comm.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        id = reader.GetInt32(0);
                    }
                }
            }
            connection.Close();
            return id > 0 ? true : false;
        }

        // Начальная и конечная "скобки" транзакции. В серединке должны использоваться SQL-команды ТОЛЬКО на основе команды runcommand
        private DbCommand RunStart()
        {
            if (connection.State == System.Data.ConnectionState.Open) connection.Close();
            connection.Open();
            DbCommand runcommand = connection.CreateCommand();
            runcommand.CommandType = System.Data.CommandType.Text;
            DbTransaction transaction = connection.BeginTransaction();
            runcommand.Transaction = transaction;
            runcommand.CommandTimeout = 600000;
            return runcommand;
        }
        private void RunStop(DbCommand runcommand)
        {
            runcommand.Transaction.Commit();
            connection.Close();
        }



        public override string ToString()
        {
            return "SQLite";
        }
    }
}
