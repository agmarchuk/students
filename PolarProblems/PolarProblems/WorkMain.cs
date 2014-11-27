using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Windows;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace PolarProblems
{
    class WorkMain
    {
        private string path = @"../../../Databases/";
        private PType tp_bd;
        private PaCell table_bd;
        private BinaryTree BTInd;

        private MySQL dbmysql;
        private SQLite dbsqlite;
        private SQLdatabase dbms;

//PolarDB-------------------------------------------------------------------------------
        //Инициализация БД
        public void InitPolar()
        {
             PType tp_bd = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("birthdate", new PType(PTypeEnumeration.longinteger))
            );
            
            PaCell table_bd_init = new PaCell(new PTypeSequence(tp_bd), path + "birthdates.pac", false);
 
            this.table_bd = table_bd_init;
            this.tp_bd = tp_bd;
          }

        //Разогрев БД
        public void PolarWarmUp()
        {
            foreach (var v in table_bd.Root.ElementValues()) ;
        }

        private PaEntry TestSearch(BinaryTree cell, string name)
        {
            PaEntry entry = table_bd.Root.Element(0);
            bool Founded = false;

            PxEntry found = cell.BinarySearch(pe =>
            {
                entry.offset = (long)pe.Get();
                string s = (string)entry.Field(0).Get();
                //TODO: кривой способ определения результата поиска
                if (s == name) Founded = true;

                return String.Compare(name, s, StringComparison.Ordinal);
            });

            if (Founded == true) entry.offset = (long)found.Get();
            else entry.offset=0;

            return entry;
        } 

        //Наполнение данными БД
        public bool GoLoadPolar(int Npotok)
        {
            if (table_bd.IsEmpty)
            {
                table_bd.Clear();
                table_bd.Fill(new object[0]);
            }

            //Генерируем поток записей 
            Random rnd = new Random(Npotok);
            for (int i = 1; i <= Npotok; ++i)
            {
                table_bd.Root.AppendElement(new object[] { "Вася" + rnd.Next(Npotok-1), 
                    DateTime.Today.AddDays(-rnd.Next(7000)).ToBinary() });
            }
            table_bd.Flush();
            // проверяем содержимое
            //Console.WriteLine(new PTypeSequence(tp_bd).Interpret(table_bd.Root.Get()));

             
            Func<object, PxEntry, int> edepth = (object v1, PxEntry en2) =>
            {
                PaEntry entry = table_bd.Root.Element(0);
                long index = (long)en2.Get();

                entry.offset = (long)v1;

                //сравниваем поля записей из опорной таблицы
                string name1 = (string)entry.Field(0).Get();

                entry.offset = index;
                string name2 = (string)entry.Field(0).Get();

                return String.Compare(name1, name2, StringComparison.Ordinal);//вернётся: -1,0,1
            };

            PType tp_btr = new PType(PTypeEnumeration.longinteger);
            //создание индексов в виде бинарного дерева
            BinaryTree BtreeInd = new BinaryTree(tp_btr,
                edepth, path + "btree.pxc", readOnly: false);
            
            this.BTInd = BtreeInd;

            foreach (PaEntry ent in table_bd.Root.Elements())
            {
                long offset = ent.offset;
                BtreeInd.Add(offset);
            }
            
            return true; 
        }

        public bool GoSearchPolar(string srch)
        {
            PaEntry entt = table_bd.Root.Element(0);
            entt = TestSearch(BTInd, "Вася123");

            if (entt.offset == 0) return false;

            //string s = (string)entt.Field(0).Get();
            //MessageBox.Show(s);

            //TODO: определить когда закрывать БД
            table_bd.Close();
            BTInd.Close();
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
                SQLdatabase db = new SQLdatabase(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\My_Documents\Coding\_VSprojects\students\PolarProblems\PolarProblems\birthdates_mssql.mdf ;Integrated Security=True;Connect Timeout=30");
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

        public void FinalExit()
        {
            try
            {
                File.Delete(path + "btree.pxc");
                File.Delete(path + "birthdates.pac");
                File.Delete(path + "birthdates_sqlite.db3");
                File.Delete(path + "birthdates.pxc");
                File.Delete(path + "index_birth.pac");
                File.Delete(path + "index_birth_s.pac");
                File.Delete(path + "index_name.pac");
                File.Delete(path + "index_name_s.pac");
            }
            catch (Exception ex)
            {
            }
        }
    }
}
