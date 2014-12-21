using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Xml.Linq;
using System.Windows;

namespace PolarProblems
{
    public class SQLite
    {
        private DbConnection connection = null;
        public SQLite(string connectionstring)
        {
            DbProviderFactory fact = new System.Data.SQLite.SQLiteFactory(); //DbProviderFactories.GetFactory("System.Data.SQLite");
            this.connection = fact.CreateConnection();
            this.connection.ConnectionString = connectionstring;
            //using (DbConnection cnn = fact.CreateConnection())
            //{
            //    cnn.ConnectionString = "Data Source=D:\\home\\dev2012\\PolarLearning\\Task08SimpleGraph\\test.db3";
            //    cnn.Open();
            //    connection = cnn;
        }
        public void PrepareToLoad()
        {
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandText = "DROP TABLE birthdates;";
            string message = null;
            try { comm.ExecuteNonQuery(); }
            catch (Exception ex) { message = ex.Message; }
            comm.CommandText =
@"CREATE TABLE birthdates(id INT NOT NULL, name NVARCHAR(255), birth INT, PRIMARY KEY(id));";
            try { comm.ExecuteNonQuery(); }
            catch (Exception ex) { message = ex.Message; }
            connection.Close();
            //if (message != null) MessageBox.Show(message);

        }

        public void MakeIndexes()
        {
            connection.Open();
            DbCommand comm = connection.CreateCommand();
            comm.CommandTimeout = 2000;
            comm.CommandText =
@"CREATE INDEX person_name ON birthdates(name);
CREATE INDEX person_birth ON birthdates(birth);";
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            connection.Close();
        }

        public bool LoadElementFlow(IEnumerable<XElement> element_flow)
        {
            DbCommand runcomm = RunStart();
            int i = 1;
            foreach (XElement element in element_flow)
            {
                i++;
                string table = element.Name.LocalName;
                string valqry = null;

                if (table == "birthdates")
                {
                    valqry = element.Attribute("id").Value + ", " +
                    "'" + element.Element("name").Value.Replace('\'', '"') + "'," +
                    "" + element.Element("birth").Value;

                    runcomm.CommandText = "INSERT INTO " + table + " VALUES (" + valqry + ");";
                    runcomm.ExecuteNonQuery();
                }
            }
            RunStop(runcomm);
            if (i == 1) return false; else return true;
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
        //public void SelectById(int id, string table)
        //{
        //    connection.Open();
        //    var comm = connection.CreateCommand();
        //    comm.CommandText = "SELECT * FROM " + table + " WHERE id=" + id + ";";
        //    var reader = comm.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        var oname = reader.GetValue(1);
        //        string name = reader.GetString(1);
        //        Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
        //    }
        //    connection.Close();
        //}
        public bool SearchByName(string searchstring, string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            //comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE N'" + searchstring + "%'";
            comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE '" + searchstring + "%'";
            var reader = comm.ExecuteReader();
            bool rez;

            //while (reader.Read())
            //{
            //    var oname = reader.GetValue(1);
            //    string name = reader.GetString(1);
            //    //Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));

            //}
            if (reader.Read()) rez = true; else rez = false;
            connection.Close();
            return rez;
        }

        public bool SearchByNameFirst(string searchstring, string table)
        {
            connection.Open();
            var comm = connection.CreateCommand();
            //comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE N'" + searchstring + "%'";
            comm.CommandText = "SELECT * FROM " + table + " WHERE name LIKE '" + searchstring + "%' LIMIT 1,1";
            var reader = comm.ExecuteReader();
            bool rez;

            //while (reader.Read())
            //{
            //    var oname = reader.GetValue(1);
            //    string name = reader.GetString(1);
            //    //Console.WriteLine("id={0} name={1} fd={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));

            //}
            if (reader.Read()) rez = true; else rez = false;
            connection.Close();
            return rez;
        }

        //public int GetRelationByPerson(int id)
        //{
        //    connection.Open();
        //    var comm = connection.CreateCommand();
        //    comm.CommandText = "SELECT photo_doc.id,photo_doc.name FROM reflection INNER JOIN photo_doc ON reflection.in_doc=photo_doc.id WHERE reflection.reflected=" + id + ";";
        //    var reader = comm.ExecuteReader();
        //    int cnt = 0;
        //    while (reader.Read())
        //    {
        //        //Console.WriteLine("v0={0} v1={1} v2={2}", reader.GetValue(0), reader.GetValue(1), reader.GetValue(2));
        //        cnt++;
        //    }
        //    connection.Close();
        //    //Console.WriteLine("cnt={0}", cnt);
        //    return cnt;
        //}


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
