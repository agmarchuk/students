using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using ExtendedIndexBTree;
using System.IO;

namespace TestPlatform.SpeedTests
{
    class TestPolarDB : IPerformanceTest
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        #region Comparers
        private Func<object, object, int> stringElementComparer = (object ob1, object ob2) =>
        {
            object[] node1 = (object[])ob1;
            int hash1 = (int)node1[1];

            object[] node2 = (object[])ob2;
            int hash2 = (int)node2[1];

            if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
            {
                PaEntry entry = Books.Root.Element(0);
                long offset1 = (long)node1[0];
                long offset2 = (long)node2[0];

                entry.offset = offset1;
                object[] pair1 = (object[])entry.Get();
                string key1 = (string)pair1[1];

                entry.offset = offset2;
                object[] pair2 = (object[])entry.Get();
                string key2 = (string)pair2[1];

                return String.Compare(key1, key2, StringComparison.Ordinal);//вернётся: -1,0,1
            }
            else
                return ((hash1 < hash2) ? -1 : 1);

        };

        private Func<object, object, int> intElementComparer = (object ob1, object ob2) =>
        {
            object[] node1 = (object[])ob1;
            int value1 = (int)node1[1];

            object[] node2 = (object[])ob2;
            int value2 = (int)node2[1];

            if (value1 == value2) return 0;

            return ((value1 < value2) ? -1 : 1);
        };

        private Func<object, object, int> intKeyComparer = (object ob1, object ob2) =>
        {
            int value1 = (int)ob1;

            object[] node2 = (object[])ob2;
            int value2 = (int)node2[1];

            if (value1 == value2) return 0;

            return ((value1 < value2) ? -1 : 1);
        };

        private Func<object, object, int> stringKeyComparer = (object ob1, object ob2) =>
        {
            string value1 = (string)ob1;
            int hash1 = value1.GetHashCode();

            object[] node2 = (object[])ob2;
            int hash2 = (int)node2[1];

            if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
            {
                PaEntry entry = Books.Root.Element(0);
                entry.offset = (long)node2[0];
                object[] pair2 = (object[])entry.Get();
                string value2 = (string)pair2[1];

                return String.Compare(value1, value2, StringComparison.Ordinal);//вернётся: -1,0,1
            }
            else
                return ((hash1 < hash2) ? -1 : 1);
        };
        #endregion

        static PaCell Books;
        BTreeInd id_author_index, title_index;
        string path = "../../../../Databases/";

        public long CreateDB(int size)
        {
            sw.Reset();
            Random rnd = new Random();

            //задаём тип для записи в ячейку БД
            PType TBooks = new PTypeSequence(
                new PTypeRecord(
                    new NamedType("id", new PType(PTypeEnumeration.integer)),
                    new NamedType("title", new PType(PTypeEnumeration.sstring)),
                    new NamedType("pages", new PType(PTypeEnumeration.integer)),
                    new NamedType("id_author", new PType(PTypeEnumeration.integer))
                )
            );

            //создаём БД
            Books = new PaCell(TBooks, path + "Books.pac", false);
            //очистка БД
            Books.Clear();
            Books.Fill(new object[0]);

            PType tp_id_author_index = new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("id", new PType(PTypeEnumeration.integer))
            );

            //узел дерева состоит из офсета и хешкода
            PType tp_title_index = new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("hash", new PType(PTypeEnumeration.integer))
            );

            id_author_index = new BTreeInd(tp_id_author_index, intKeyComparer, intElementComparer, path + "id_author_index.pxc");
            title_index = new BTreeInd(tp_title_index, stringKeyComparer, stringElementComparer, path + "title_index.pxc");

            for (int i = 0; i < size; ++i)
            {
                int id_author = (rnd.Next(size) + rnd.Next(size)) % size;
                string title = "book" + i;
                object book = new object[]
                {
                    i,
                    title,
                    1001,
                    id_author
                };
                sw.Start();
                long off = Books.Root.AppendElement(book);
                id_author_index.AppendElement(new object[] { off, id_author });
                title_index.AppendElement(new object[] { off, title.GetHashCode() });
                sw.Stop();
            }

            Books.Flush();
            return sw.ElapsedMilliseconds;
        }

        public void DeleteDB()
        {
            Books.Close();
            id_author_index.Close();
            title_index.Close();
            File.Delete(path + "id_author_index.pxc");
            File.Delete(path + "title_index.pxc");
            File.Delete(path + "Books.pac");
        }

        public long FindFirst(int repeats, string fieldName)
        {
            sw.Reset();

            long offset = 0;
            Random rnd = new Random();
            int N = (int)Books.Root.Count();

            if (fieldName == "title")
                for (int i = 0; i < repeats; ++i)
                {
                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
                    sw.Start();
                    offset = title_index.FindFirst((object)("book" + r));
                    sw.Stop();
                }
            else
                for (int i = 0; i < repeats; ++i)
                {
                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
                    sw.Start();
                    offset = id_author_index.FindFirst((object)r);
                    sw.Stop();
                }

            return sw.ElapsedMilliseconds;
        }

        public long FindAll(int repeats, string fieldName)
        {
            sw.Reset();

            List<long> offsets = new List<long>();
            Random rnd = new Random();
            int N = (int)Books.Root.Count();

            if (fieldName == "title")
                for (int i = 0; i < repeats; ++i)
                {
                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
                    sw.Start();
                    offsets = title_index.FindAll((object)("book" + r)).ToList<long>();
                    sw.Stop();
                }
            else
                for (int i = 0; i < repeats; ++i)
                {
                    int r = (rnd.Next(N) + rnd.Next(N)) % N;
                    sw.Start();
                    offsets = id_author_index.FindAll((object)r).ToList<long>();
                    sw.Stop();
                }
            return sw.ElapsedMilliseconds;
        }

        public long WarmUp()
        {
            sw.Reset();
            Random rnd = new Random();
            int N = (int)Books.Root.Count();
            List<long> offsets = new List<long>();

            foreach (var book in Books.Root.ElementValues()) { }

            for (int i = 0; i < 1000; ++i)
            {
                int r = (rnd.Next(N) + rnd.Next(N)) % N;
                sw.Start();
                offsets = id_author_index.FindAll((object)r).ToList<long>();
                offsets = title_index.FindAll((object)("book" + r)).ToList<long>();
                sw.Stop();
            }
            return sw.ElapsedMilliseconds;
        }
    }
}
