using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TableOfNamesSQL
{
    class MSSQL : ISQLDB
    {
        private DbConnection connection = null;
        private string dbName = "";
        private string path = "";

        public MSSQL(string connectionstring, string path, string dbName)
        {
            string dataprovider = "System.Data.SqlClient";
            DbProviderFactory factory = DbProviderFactories.GetFactory(dataprovider);
            connection = factory.CreateConnection();
            connection.ConnectionString = connectionstring;

            this.dbName = dbName;
            this.path = path;

            CreateDB();
        }

        public void CreateDB()
        {
            connection.Open();
            string[] files = { path + dbName + ".mdf", path + dbName + ".ldf" };

            var query = "CREATE DATABASE " + dbName +
            " ON PRIMARY" +
            " (NAME = " + dbName + "_data," +
            " FILENAME = '" + files[0] + "'," +
            " SIZE = 4MB," +
            " MAXSIZE = 1024MB," +
            " FILEGROWTH = 50%)" +

            " LOG ON" +
            " (NAME = " + dbName + "_log," +
            " FILENAME = '" + files[1] + "'," +
            " SIZE = 1MB," +
            " MAXSIZE = 1024MB," +
            " FILEGROWTH = 50%)" +
            ";";

            DbCommand comm = connection.CreateCommand();
            comm.CommandText = query;
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception) { }
            finally
            {
                connection.Close();
            }
        }

        public void Delete()
        {
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "DROP DATABASE " + dbName + ";";

            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception) { }
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public void PrepareToLoad()
        {
            connection.Open();

            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "DROP TABLE TestStrings;";

            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception) {}

            comm.CommandText =
            @"CREATE TABLE TestStrings (id INTEGER PRIMARY KEY NOT NULL IDENTITY (0,1), 
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
            connection.Open();

            DbCommand comm = connection.CreateCommand();
            comm.CommandTimeout = 6000000;
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
            connection.Open();

            DbCommand runcomm = RunStart();
            Random rnd = new Random();

            for (int i = 0; i < NumberData; ++i)
            {
                string names = "s" + rnd.Next(2*NumberData); ;
                runcomm.CommandText = "INSERT INTO TestStrings(name) VALUES ('" + names + "');";
                runcomm.ExecuteNonQuery();
            }

            RunStop(runcomm);
        }

        public void Count(string table)
        {
            connection.Open();

            var comm = connection.CreateCommand();
            //comm.CommandTimeout = 1000;
            comm.CommandText = "SELECT COUNT(*) FROM " + table +";";
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Кол-во записей = {0}", obj);
            connection.Close();
        }
        public void SelectByIdAll(int id, string table)
        {
            connection.Open();

            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE id=" + id + ";";
            //var reader = comm.ExecuteReader();
            //Console.WriteLine("id={0} name={1} deleted={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Кол-во найденных: {0}", obj);
            connection.Close();
        }

        public bool SelectByIdFirst(int id, string table)
        {
            connection.Open();

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
            connection.Open();

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
            connection.Open();

            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT TOP 1 id FROM " + table + " WHERE name LIKE '" + searchstring + "%'";

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
            runcommand.CommandTimeout = 6000000;
            return runcommand;
        }
        private void RunStop(DbCommand runcommand)
        {
            runcommand.Transaction.Commit();
            connection.Close();
        }

        public override string ToString()
        {
            return "MS SQL";
        }
    }
}
