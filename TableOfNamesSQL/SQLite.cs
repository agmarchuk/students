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
            this.connection.ConnectionString = connectionString;
        }

        public void Delete()
        {
            connection.Close();
            //удаление БД только через удаление файла
            //TODO: починить удаление, не освобождается файл
            try
            {
                System.IO.File.Delete(path + "sqlite.db3");
            }
            catch (Exception) { /*throw;*/ }
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public void PrepareToLoad()
        {
            OpenConnection();

            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "DROP TABLE TestStrings;";

            try
            {
                comm.ExecuteNonQuery(); 
            }
            catch (Exception) {}

            comm.CommandText =
            @"CREATE TABLE TestStrings (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
                                        name NVARCHAR(255), 
                                        deleted INTEGER DEFAULT 0);";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception) { throw; }
            finally { connection.Close(); }
        }

        public void MakeIndexes()
        {
            OpenConnection();

            DbCommand comm = connection.CreateCommand();
            //comm.CommandTimeout = 2000;
            comm.CommandText ="CREATE INDEX index_name ON TestStrings(name);";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception) { throw; }
            finally
            {
                connection.Close();
            }
        }

        public void LoadElementFlow(int NumberData)
        {
            OpenConnection();

            DbCommand runcomm = RunStart();
            Random rnd = new Random();

            for (int i = 0; i < NumberData; ++i)
            {
                string names = "s" + rnd.Next(2 * NumberData); ;
                runcomm.CommandText = "INSERT INTO TestStrings(name) VALUES ('" + names +"');";
                runcomm.ExecuteNonQuery();
            }

            RunStop(runcomm);
        }

        public void Count(string table)
        {
            OpenConnection();

            var comm = connection.CreateCommand();
            //comm.CommandTimeout = 1000;
            comm.CommandText = "SELECT COUNT(*) FROM " + table + ";";
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Кол-во записей = {0}", obj);
            connection.Close();
        }
        public void SelectByIdAll(int id, string table)
        {
            OpenConnection();

            var comm = connection.CreateCommand();
            //comm.CommandTimeout = 1000;
            comm.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE id=" + id + ";";
            //var reader = comm.ExecuteReader();
            //Console.WriteLine("id={0} name={1} deleted={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Кол-во найденных: {0}", obj);
            connection.Close();
        }

        public bool SelectByIdFirst(int id, string table)
        {
            OpenConnection();

            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT name FROM " + table + " WHERE id=" + id + ";";
            var reader = comm.ExecuteReader();
            
            string name = "";
            if (reader.Read())
            {
                name = reader.GetString(0);
            }
            connection.Close();
            return name != "" ? true : false;
        }

        public void SearchByNameAll(string searchstring, string table)
        {
            OpenConnection();

            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE name LIKE '" + searchstring + "%'";
            //var reader = comm.ExecuteReader();
            //Console.WriteLine("id={0} name={1} deleted={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Кол-во найденных: {0}", obj);
            connection.Close();
        }

        public bool SearchByNameFirst(string searchstring, string table)
        {
            OpenConnection();

            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT id FROM " + table + " WHERE name LIKE '" + searchstring + "%' LIMIT 1,1";
            
            var reader = comm.ExecuteReader();
            int id = 0;
            if (reader.Read())
            {
                id = reader.GetInt32(0);
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
            runcommand.CommandTimeout = 10000;
            return runcommand;
        }
        private void RunStop(DbCommand runcommand)
        {
            runcommand.Transaction.Commit();
            connection.Close();
        }

        private void OpenConnection()
        {
            try
            {
                connection.Open();
            }
            catch (Exception) { connection.Close(); }
        }

        public override string ToString()
        {
            return "SQLite";
        }
    }
}
