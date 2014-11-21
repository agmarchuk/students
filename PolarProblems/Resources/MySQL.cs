using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PolarProblems
{
    public class MySQL
    {
        DbConnection connection = null;
        public MySQL(string connectionstring)
        {
            DbProviderFactory fact = new MySql.Data.MySqlClient.MySqlClientFactory(); //DbProviderFactories.GetFactory("System.Data.SQLite");
            this.connection = fact.CreateConnection();
            this.connection.ConnectionString = connectionstring;
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "USE test;";
            try { comm.ExecuteNonQuery(); }
            catch (Exception) {}
            connection.Close();
        }
        public void PrepareToLoad()
        {
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "DROP TABLE person; DROP TABLE photo_doc; DROP TABLE reflection;";
            string message = null;
            try { comm.ExecuteNonQuery(); }
            catch (Exception ex) { message = ex.Message; }
            comm.CommandText =
@"CREATE TABLE person (id INT NOT NULL, name NVARCHAR(400), age INT, PRIMARY KEY(id));
CREATE TABLE photo_doc (id INT NOT NULL, name NVARCHAR(400), PRIMARY KEY(id));
CREATE TABLE reflection (id INT NOT NULL, reflected INT NOT NULL, in_doc INT NOT NULL);";
            try { comm.ExecuteNonQuery(); }
            catch (Exception ex) { message = ex.Message; }
            connection.Close();
            if (message != null) Console.WriteLine(message);

        }

        public void MakeIndexes()
        {
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandTimeout = 2000;
            comm.CommandText =
@"CREATE INDEX person_name ON person(name);
CREATE INDEX reflection_reflected ON reflection(reflected);
CREATE INDEX reflection_indoc ON reflection(in_doc);";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            connection.Close();
        }

        public void LoadElementFlow(IEnumerable<XElement> element_flow)
        {
            DbCommand runcomm = RunStart();
            int i = 1;
            foreach (XElement element in element_flow)
            {
                if (i % 1000 == 0) Console.Write("{0} ", i / 1000);
                i++;
                string table = element.Name.LocalName;
                string aaa = null;
                if (table == "person")
                    aaa = "(" + element.Attribute("id").Value + ", " +
                        //"N'" + element.Element("name").Value.Replace('\'', '"') + "', " +
                        //"'" + element.Element("name").Value.Replace('\'', '"') + "', " +
                        "'" + element.Element("name").Value.Replace('\'', '"').Replace("Пупкин", "Pupkin") + "', " +
                        "" + element.Element("age").Value + ");";
                else if (table == "photo_doc")
                    aaa = "(" + element.Attribute("id").Value + ", " +
                        "N'" + element.Element("name").Value.Replace('\'', '"') + "'" +
                        //"'" + element.Element("name").Value.Replace('\'', '"') + "'" +
                        ")";
                else if (table == "reflection")
                    aaa = "(" + element.Attribute("id").Value + ", " +
                        "" + element.Element("reflected").Attribute("ref").Value + ", " +
                        "" + element.Element("in_doc").Attribute("ref").Value + "" +
                        ")";
                runcomm.CommandText = "INSERT INTO " + table + " VALUES " + aaa + ";";
                runcomm.ExecuteNonQuery();
            }
            RunStop(runcomm);
            Console.WriteLine();
        }

        public void Count(string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandTimeout = 1000;
            comm.CommandText = "SELECT COUNT(*) FROM " + table + ";";
            var obj = comm.ExecuteScalar();
            Console.WriteLine("Count()={0}", obj);
            connection.Close();
        }
        public void SelectById(int id, string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT * FROM " + table + " WHERE id=" + id + ";";
            var reader = comm.ExecuteReader();
            while (reader.Read())
            {
                var oname = reader.GetValue(1);
                string name = reader.GetString(1);
                Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            }
            connection.Close();
        }
        public void SearchByName(string searchstring, string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE N'" + searchstring + "%'";
            //comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE '" + searchstring + "%'";
            var reader = comm.ExecuteReader();
            while (reader.Read())
            {
                var oname = reader.GetValue(1);
                string name = reader.GetString(1);
                Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
            }
            connection.Close();
        }
        public int GetRelationByPerson(int id)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            comm.CommandText = "SELECT photo_doc.id,photo_doc.name FROM reflection INNER JOIN photo_doc ON reflection.in_doc=photo_doc.id WHERE reflection.reflected=" + id + ";";
            var reader = comm.ExecuteReader();
            int cnt = 0;
            while (reader.Read())
            {
                //Console.WriteLine("v0={0} v1={1} v2={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
                cnt++;
            }
            connection.Close();
            //Console.WriteLine("cnt={0}", cnt);
            return cnt;
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
    }
}
