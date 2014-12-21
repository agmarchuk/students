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
using Microsoft.Office.Interop.Excel;
using System.Globalization;

namespace PolarProblems
{
    class WorkMain
    {
        public string path = @"../../../Databases/";
        private PType tp_bd;
        private PaCell table_bd;
        private BinaryTreeIndex BTInd_name;
        private BinaryTreeIndex BTInd_birth;

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


        private PaEntry TestSearch(BinaryTreeIndex cell, object ob, int ifield)
        {
            PaEntry entry = table_bd.Root.Element(0);
            bool Founded = false;

            PxEntry found = cell.BinarySearch(pe =>
            {
                entry.offset = (long)pe.Get();

                object get = entry.Field(ifield).Get();

                int rezcmp = cell.elementCompare(ob, get);
                if (rezcmp == 0) Founded = true;

                return rezcmp;
            });
            if (Founded == true) entry.offset = (long)found.Get();
            else entry.offset=0;

            return entry;
        }

        private IEnumerable<PaEntry> TestSearchAll(BinaryTreeIndex cell, object ob, int ifield)
        {
            PaEntry entry = table_bd.Root.Element(0);

            var found = cell.Root.Elements()
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                });
            return found;
        }

       
        //Наполнение данными БД
        public bool GoLoadPolar(int Npotok)
        {
            if (table_bd.IsEmpty)
            {
                table_bd.Clear();
                table_bd.Fill(new object[0]);
            }

            //Генерируем данные
            TestDataGenerator generator;
            generator = new TestDataGenerator(Npotok, 77777);

            foreach (TestDataGenerator.Birthdates element in generator.Generate2())
            {
                string name = element.name;
                long birth = element.birthdate;

                table_bd.Root.AppendElement(new object[] { name, birth });
            }
            table_bd.Flush();
            // проверяем содержимое
            //Console.WriteLine(new PTypeSequence(tp_bd).Interpret(table_bd.Root.Get()));

            
            Func<object, object, int> compare_name = (object ob1, object ob2) =>
            {
                return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
            };

            Func<object, object, int> compare_birth = (object ob1, object ob2) =>
            {
                //сравниваем поля записей из опорной таблицы
                long birth1 = (long)ob1;
                long birth2 = (long)ob2;

                if (birth1 < birth2) return -1;
                if (birth1 > birth2) return 1;
                return 0;
            };

            PType tp_btr = new PType(PTypeEnumeration.longinteger);

            //создание индексов в виде бинарного дерева для "name"
            BinaryTreeIndex BtreeInd_name = new BinaryTreeIndex(tp_btr, table_bd, 0,
                compare_name, path + "btree_name.pxc", readOnly: false);

            //создание индексов в виде бинарного дерева для "name"
            BinaryTreeIndex BtreeInd_birth = new BinaryTreeIndex(tp_btr, table_bd, 1,
                compare_birth, path + "btree_birth.pxc", readOnly: false);

            this.BTInd_name = BtreeInd_name;
            this.BTInd_birth = BtreeInd_birth;

            BtreeInd_name.Clear();
            BtreeInd_birth.Clear();
            

             foreach (PaEntry ent in table_bd.Root.Elements())
            {
                long offset = ent.offset;
                BtreeInd_name.Add(offset);
            }
             
             foreach (PaEntry ent in table_bd.Root.Elements())
            {
                long offset = ent.offset;
                BtreeInd_birth.Add(offset);
            }


             /*            
                //Далее происходит инициализация В-дерева и заполнение его индексами (офсетами на записи в опорной таблице)
                Func<object, object, int> compare_name = (object ob1, object ob2) =>
                {
                    return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
                };

                int deg = 50; //степень дерева
                PType tp_btr = new PType(PTypeEnumeration.longinteger);

                B_tree bt = new B_tree(deg, tp_btr, table_bd,
                    compare_name, path + "btree_name.pxc", readOnly: false);

                foreach (PaEntry ent in table_bd.Root.Elements())
                {
                    long offset = ent.offset;
                    bt.Insert(bt.Root, offset);
                }
             */


             // Пробный запрос
             //PaEntry entry = table_bd.Root.Element(0);

             //var query = table_bd.Root.Elements()
             //    .Select(ent =>
             //    {
             //        table_bd.Root.Elements().Field(0);
             //        entry.offset = (long)ent.Get();
             //        return entry;
             //    });

             //query = BtreeInd_name.GetAllFrom(table_bd);
             var res2 = table_bd.Root.GetValue();

             Console.WriteLine(res2.Type.Interpret(res2.Value)); // count()=100 duration=11

             return true; 
        }

        public bool GoSearchPolar_name(string srch)
        {
            PaEntry entt = table_bd.Root.Element(0);
            entt = TestSearch(BTInd_name, srch, 0);//вернется офсет на первый найденный элемент в БД

            if (entt.offset == 0) return false;

            //string s = (string)entt.Field(0).Get();
            //MessageBox.Show(s);

            long count = TestSearchAll(BTInd_name, srch, 0).Count();
            Console.WriteLine("count()={0}", count); // count()=100
            return true;
        }
        public bool GoSearchPolar_birth(long brth)
        {
            PaEntry entt = table_bd.Root.Element(0);
            entt = TestSearch(BTInd_birth, brth, 1);

            if (entt.offset == 0) return false;

            //string s = DateTime.FromBinary((long)entt.Field(1).Get()).ToString();
            //MessageBox.Show(s);

            //TODO: определить когда закрывать БД

            return true;
        }



//MySQL-------------------------------------------------------------------------------
        public void InitMySql()
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
        public void InitSQLite()
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
            return dbsqlite.SearchByNameFirst(srch, "birthdates");
        }

//MS SQL-------------------------------------------------------------------------------
        public void InitMSSQL()
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
                //table_bd.Close();
                //BTInd_name.Close();
                //BTInd_birth.Close();

                //File.Delete(path + "btree.pxc");
                //File.Delete(path + "birthdates.pac");
                File.Delete(path + "birthdates_sqlite.db3");
                //File.Delete("birthdates_mssql_log.ldf");
                //File.Delete(path + "birthdates.pxc");
                //File.Delete(path + "index_birth.pac");
                //File.Delete(path + "index_birth_s.pac");
                //File.Delete(path + "index_name.pac");
                //File.Delete(path + "index_name_s.pac");
                //File.Delete(path + "btree_birth.pxc");
                //File.Delete(path + "btree_name.pxc");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Отображает грфик в EXEL, но не сохраняет его. 
        /// </summary>
        /// <param name="xy">корневой массив-линий, листовой точек. Точки должны отличться на одну постоянноую величину</param>
        public void Draw(int[][] xy)
        {
            Microsoft.Office.Interop.Excel.Application application = new Microsoft.Office.Interop.Excel.Application() { Visible = true };
            var workbooks = application.Workbooks;
            var wordBook = workbooks.Open(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\chart.xls");
            var sheet = (_Worksheet)wordBook.ActiveSheet;
            var chart = (_Chart)wordBook.Charts.Add();
            chart.Name = "Cкорость от объёма";
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            sheet.ClearArrows();
            for (int j = 0; j < xy.Length; j++)
                for (int i = 0; i < xy[0].Length; i++)
                {
                    {
                        sheet.Cells[i + 1, j + 1] = xy[j][i].ToString(CultureInfo.InvariantCulture);
                    }
                }

            chart.ChartWizard(sheet.Range["A1", "G" + xy[0].Length], XlChartType.xlLine);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(chart);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(sheet);
            //wordBook.Close(false);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(wordBook);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(workbooks);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(application);
        }
        public void Draw2(int[] xy)
        {
            Microsoft.Office.Interop.Excel.Application application = new Microsoft.Office.Interop.Excel.Application() { Visible = true };
            var workbooks = application.Workbooks;
            var wordBook = workbooks.Open(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\chart.xls");
            var sheet = (_Worksheet)wordBook.ActiveSheet;
            var chart = (_Chart)wordBook.Charts.Add();
            chart.Name = "Cкорость от объёма";
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            sheet.ClearArrows();
            for (int j = 1; j < xy.Length; j++)
                    {
                        sheet.Cells[j, 1] = xy[j].ToString(CultureInfo.InvariantCulture);
                    }

            chart.ChartWizard(sheet.Range["A1", "G" + xy.Length], XlChartType.xlLine);
        }

    }
}
