//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using PolarDB;
//using PolarHashTable;
//using System.IO;

//namespace TestPlatform.SpeedTests
//{
//    public class TestHashTableIndexPolar : IPerformanceTest
//    {
//        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
//        private string path = "../../../Databases/";

//        private PType tp, hashType;
//        public static PaCell booksDB;
//        public HTIndex title_index;

//        #region Comparers
//        private Func<object, object, int> stringElementComparer = (object ob1, object ob2) =>
//        {
//            object[] node1 = (object[])ob1;
//            int hash1 = (int)node1[1];

//            object[] node2 = (object[])ob2;
//            int hash2 = (int)node2[1];

//            if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
//            {
//                PaEntry entry = booksDB.Root.Element(0);
//                long offset1 = (long)node1[0];
//                long offset2 = (long)node2[0];

//                entry.offset = offset1;
//                object[] pair1 = (object[])entry.Get();
//                string key1 = (string)pair1[1];

//                entry.offset = offset2;
//                object[] pair2 = (object[])entry.Get();
//                string key2 = (string)pair2[1];

//                return String.Compare(key1, key2, StringComparison.Ordinal);//вернётся: -1,0,1
//            }
//            else
//                return ((hash1 < hash2) ? -1 : 1);

//        };

//        private Func<object, object, int> intElementComparer = (object ob1, object ob2) =>
//        {
//            object[] node1 = (object[])ob1;
//            int value1 = (int)node1[1];

//            object[] node2 = (object[])ob2;
//            int value2 = (int)node2[1];

//            if (value1 == value2) return 0;

//            return ((value1 < value2) ? -1 : 1);
//        };

//        private Func<object, object, int> intKeyComparer = (object ob1, object ob2) =>
//        {
//            int value1 = (int)ob1;

//            object[] node2 = (object[])ob2;
//            int value2 = (int)node2[1];

//            if (value1 == value2) return 0;

//            return ((value1 < value2) ? -1 : 1);
//        };

//        private Func<object, object, int> stringKeyComparer = (object ob1, object ob2) =>
//        {
//            string value1 = (string)ob1;
//            int hash1 = value1.GetHashCode();

//            object[] node2 = (object[])ob2;
//            int hash2 = (int)node2[1];

//            if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
//            {
//                PaEntry entry = booksDB.Root.Element(0);
//                entry.offset = (long)node2[0];
//                object[] pair2 = (object[])entry.Get();
//                string value2 = (string)pair2[1];

//                return String.Compare(value1, value2, StringComparison.Ordinal);//вернётся: -1,0,1
//            }
//            else
//                return ((hash1 < hash2) ? -1 : 1);
//        };
//        #endregion

//        long IPerformanceTest.CreateDB(int N)
//        {
//            sw.Reset();

//            //задаём тип для записи в ячейку БД
//            tp = new PTypeSequence(new PTypeRecord(
//                new NamedType("id", new PType(PTypeEnumeration.longinteger)),
//                new NamedType("title", new PType(PTypeEnumeration.sstring)),
//                new NamedType("id_author", new PType(PTypeEnumeration.longinteger)),
//                new NamedType("deleted", new PType(PTypeEnumeration.boolean)))
//            );

//            if (!System.IO.File.Exists(path + "Books.pac"))
//            {
//                //создаём БД
//                booksDB = new PaCell(tp, path + "Books.pac", false);
//                //очистка БД
//                booksDB.Clear();
//                booksDB.Fill(new object[0]);
//            }
//            else
//            {
//                //открываем БД
//                booksDB = new PaCell(tp, path + "Books.pac", false);
//            }

//            hashType = new PTypeRecord(
//                //new NamedType("offset", new PType(PTypeEnumeration.longinteger)),//указатель на определенный лист в листе оффсетов на ключи опорной таблицы
//                new NamedType("hash", new PType(PTypeEnumeration.integer))
//            );
//            title_index = new HTIndex(hashType, stringKeyComparer, booksDB, path);

//            if (System.IO.File.Exists(path + "offsets.dat"))
//            {
//                title_index.OpenFromBinaryFile();
//            }
//            else
//            {
//                //title_index.RestoreOffsets(booksDB.Root.Elements());
//            }

//            bool f = true;
//            if (f == true)
//            {
//                for (int i = 0; i < N; ++i)
//                {
//                    string temp = "book" + i;
//                    long newCode = booksDB.Root.Count();
//                    sw.Start();
//                    long offset = booksDB.Root.AppendElement(new object[] { newCode, temp, (long)i, false });
//                    title_index.AppendElement(new object[] { offset, (int)title_index.GetHashCode(temp) });
//                    sw.Stop();
//                }

//                sw.Start();
//                booksDB.Flush();
//                title_index.Flush();
//                sw.Stop();
//            }

//            return sw.ElapsedMilliseconds;
//        }

//        void IPerformanceTest.DeleteDB()
//        {
//            title_index.SaveToBinaryFile();
//            booksDB.Close();
//            title_index.Close();
//            //File.Delete(path + "hashTableIndex.pac");
//            //File.Delete(path + "Books.pac");
//        }

//        long IPerformanceTest.FindAll(int repeats, string fieldName)
//        {
//            sw.Reset();

//            List<long> offsets = new List<long>();
//            Random rnd = new Random();
//            int N = (int)booksDB.Root.Count();

//            if (fieldName == "title")
//                for (int i = 0; i < repeats; ++i)
//                {
//                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
//                    sw.Start();
//                    offsets = title_index.FindAll((object)("book" + r)).ToList<long>();
//                    sw.Stop();
//                }
//            else
//                for (int i = 0; i < repeats; ++i)
//                {
//                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
//                    sw.Start();
//                    //offsets = id_author_index.FindAll((object)r).ToList<long>();
//                    sw.Stop();
//                }
//            return sw.ElapsedMilliseconds;
//        }

//        long IPerformanceTest.FindFirst(int repeats, string fieldName)
//        {
//            sw.Reset();
//            Random rnd = new Random();
//            long offset = 0;
//            int N = (int)booksDB.Root.Count();

//            if (fieldName == "title")
//                for (int i = 0; i < repeats; ++i)
//                {
//                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
//                    sw.Start();
//                    offset = title_index.FindFirst((object)("book" + r));
//                    sw.Stop();
//                }
//            else
//                for (int i = 0; i < repeats; ++i)
//                {
//                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
//                    sw.Start();
//                    //offset = id_author_index.FindFirst((object)r);
//                    sw.Stop();
//                }
//            return sw.ElapsedMilliseconds;
//        }

//        long IPerformanceTest.WarmUp()
//        {
//            sw.Start();
//                foreach (var rec in booksDB.Root.Elements()) { }
//                foreach (var rec in title_index.Root.Elements()) { }
//            sw.Stop();
//            return sw.ElapsedMilliseconds;
//        }
//    }
//}
