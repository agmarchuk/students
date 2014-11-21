using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Windows;

namespace PolarProblems
{
    class WorkMain
    {
        private string path = @"../../../Databases/";
        private PaCell table_bd;
        private BinaryTree cell;


        private MySQL dbmysql;
        private SQLite dbsqlite;
        private SQLdatabase dbms;

//PolarDB-------------------------------------------------------------------------------
        public void InitPolar()
        {
            Func<object, PxEntry, int> edepth = (object v1, PxEntry en2) =>
            {
                string s1 = (string)(((object[])v1)[0]);
                return String.Compare(s1, (string)(en2.Field(0).Get()), StringComparison.Ordinal);
            };

            PType tp_bd = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("birthdate", new PType(PTypeEnumeration.longinteger))
            );
            PType tp_seq = new PTypeSequence(tp_bd);

            PaCell table_bd_init = new PaCell(tp_seq, path + "birthdates.pac", false);
            
            this.table_bd = table_bd_init;

            BinaryTree cell_init = new BinaryTree(tp_bd,
                edepth, path + "btree.pxc", readOnly: false);
          
            this.cell = cell_init;

        }
        public bool GoLoadPolar(int Npotok)
        {
            

            //if (table_bd.IsEmpty)
            {
                table_bd.Clear();
                table_bd.Fill(new object[0]);
            }

            // Создадим индекс на поле "name" и на поле "birthdate"
            FlexIndex<string> index_name = new FlexIndex<string>(path + "index_name", table_bd.Root,
                ent => (string)ent.Field(0).Get(), null);
            FlexIndex<long> index_birth = new FlexIndex<long>(path + "index_birth", table_bd.Root,
                 ent => (long)ent.Field(1).Get(), null);

            index_name.Load();
            index_birth.Load();


/*            //Генерируем поток записей 
            Random rnd = new Random(Npotok);

            for (int i = 1; i <= Npotok; ++i)
            {
                var offset = table_bd.Root.AppendElement(new object[] { "Вася" + rnd.Next(Npotok-1), (long)(20 + rnd.Next(19)) });
                table_bd.Flush();
                index_name.AddEntry(new PaEntry(tp_bd, offset, table_bd));
            }

            table_bd.Flush();
            index_name.Load();

            object from_rec = table_bd.Root.Get();
*/

            object[] valu =
            {
                1, 
                new object[]
                {
                    new object[] {"name1", 333L},
                    new object[]
                    {
                        1, 
                        new object[]
                        {
                            new object[] {"name0", 444L},
                            BinaryTree. Empty,
                            BinaryTree.Empty, 
                            0
                        }
                    },
                    BinaryTree.Empty, 
                    1
                }
            };
            cell.Fill2(valu);
            //cell.Root.UElementUnchecked(1).Field(0).Set(new object[] { "", "" });

            // проверяем содержимое
            var res = cell.Root.GetValue();
            //Console.WriteLine(res.Type.Interpret(res.Value));

            table_bd.Close();

            return true; 
        }

//MySQL-------------------------------------------------------------------------------
        public void InitMySql(int Npotok)
        {
            try
            {
                MySQL db = new MySQL("server=localhost;uid=root;pwd=1234;");
                this.dbmysql = db;

                dbmysql.PrepareToLoad();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public bool GoLoadMySql(int Npotok)
        {
            TestDataGenerator generator = new TestDataGenerator(Npotok, 77777);

            bool rez = dbmysql.LoadElementFlow(generator.Generate());
            dbmysql.MakeIndexes();

            return rez;
        }
        public bool GoSearchMySql(string srch)
        {
            return dbmysql.SearchByName(srch, "birthdates");
        }

//SQLite-------------------------------------------------------------------------------
        public void InitSQLite(int Npotok)
        {
            try
            {
                SQLite db = new SQLite("Data Source=" + path + "\\birthdates_sqlite.db3");
                this.dbsqlite = db;

                dbsqlite.PrepareToLoad();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public bool GoLoadSQLite(int Npotok)
        {
            TestDataGenerator generator = new TestDataGenerator(Npotok, 77777);

            bool rez = dbsqlite.LoadElementFlow(generator.Generate());
            dbsqlite.MakeIndexes();

            return rez;
        }
        public bool GoSearchSQLite(string srch)
        {
            return dbsqlite.SearchByName(srch, "birthdates");
        }

//MS SQL-------------------------------------------------------------------------------
        public void InitMSSQL(int Npotok)
        {
            try
            {
                SQLdatabase db = new SQLdatabase(@"Data Source=(LocalDB)\v11.0;AttachDbFilename="+ @"D:\My_Documents\Coding\_VSprojects\PolarProblems\DataBases\birthdates_mssql.mdf ;Integrated Security=True;Connect Timeout=30");
                this.dbms = db;

                dbms.PrepareToLoad();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public bool GoLoadMSSQL(int Npotok)
        {
            TestDataGenerator generator = new TestDataGenerator(Npotok, 77777);

            bool rez = dbms.LoadElementFlow(generator.Generate());
            dbms.MakeIndexes();

            return rez;
        }
        public bool GoSearchMSSQL(string srch)
        {
            return dbms.SearchByName(srch, "birthdates");
        }

        //static public void FinalExit()
        //{
        //    dbmysql.PrepareToLoad();
        //    dbsqlite.PrepareToLoad();
        //    dbms.PrepareToLoad();
        //}
    }
}
