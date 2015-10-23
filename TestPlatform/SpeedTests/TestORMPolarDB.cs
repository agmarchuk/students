using System;
using ORMPolar;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace TestPlatform.SpeedTests
{
    public class TestORMPolarDB:IPerformanceTest
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        Random rnd = new Random();
        string schemaPath = "schema.xml";

        UserContext uc;

        public long CreateDB(int N)
        {
            sw.Reset();
            

            uc = new UserContext(schemaPath);
            uc.Books.Clear();

            for (int i = 0; i < N; ++i)
            {
                Book book = new Book()
                {
                    id = i,
                    title = "book" + i,
                    pages = 1001,
                    id_author = (rnd.Next(N)+rnd.Next(N)) % N
                };
                sw.Start();
                uc.Books.Append(book);
                sw.Stop();
            }

            uc.Books.Flush();

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public long FindAll(string fieldName, object obj, out int count)
        {
            sw.Reset();
            sw.Start();

            List<Book> books = uc.Books.FindAll(fieldName, obj).ToList<Book>();

            sw.Stop();
            count = books.Count();
            //Console.WriteLine(count);
            return sw.ElapsedMilliseconds;
        }

        public long FindFirst(string fieldName,object obj)
        {
            sw.Reset();
            sw.Start();

            Book book = uc.Books.FindFirst(fieldName, obj);

            sw.Stop();
            //if (book != null)
            //    Console.WriteLine(book.title);
            //else
            //    Console.WriteLine("не найдена");

            return sw.ElapsedMilliseconds;
        }

        public long FindFirst(int repeats, string fieldName)
        {
            sw.Reset();

            Random rnd = new Random();
            for (int i = 0; i < repeats; ++i)
            {
                int r = rnd.Next(1000000);
                Book book;
                if (fieldName == "title")
                {
                    sw.Start();
                    book = uc.Books.FindFirst(fieldName, (object)("book"+r));
                    sw.Stop();
                }
                else
                {
                    sw.Start();
                    book = uc.Books.FindFirst(fieldName, (object)r);
                    sw.Stop();
                } 


                //if (book != null)
                //    Console.WriteLine(book.title);
                //else
                //    Console.WriteLine("не найдена");
            }

            return sw.ElapsedMilliseconds;
        }
        public long FindAll(int repeats, string fieldName)
        {
            sw.Reset();
            //count = 0;

            Random rnd = new Random();
            for (int i = 0; i < repeats; ++i)
            {
                int r = rnd.Next(1000000);
                List<Book> books;
                if (fieldName == "title")
                {
                    sw.Start();
                    books = uc.Books.FindAll(fieldName, (object)("book" + r)).ToList<Book>();
                    sw.Stop();
                }
                else
                {
                    sw.Start();
                    books = uc.Books.FindAll(fieldName, (object)r).ToList<Book>();
                    sw.Stop();
                }
                //count = books.Count();
                //Console.WriteLine(count);
            }
            
            return sw.ElapsedMilliseconds;
        }

        public void DeleteDB()
        {
            uc.Dispose();

            XDocument schema = XDocument.Load(schemaPath);

            foreach (XElement element in schema.Root.Elements()
                    .Where(el => el.Name == "class"))
            {
                string filePath = element.Attribute("path").Value;
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            if (File.Exists("Index[books]-[title].pxc"))
                File.Delete("Index[books]-[title].pxc");
            if (File.Exists("Index[books]-[id_author].pxc"))
                File.Delete("Index[books]-[id_author].pxc");

            if (File.Exists("../../../Databases" + "/Index[books]-[title].pxc"))
                File.Delete("../../../Databases" + "/Index[books]-[title].pxc");
            if (File.Exists("../../../Databases" + "/Index[books]-[id_author].pxc"))
                File.Delete("../../../Databases" + "/Index[books]-[id_author].pxc");

            if (File.Exists("../../../Databases" + "/BTreeIndex.pxc"))
                File.Delete("../../../Databases" + "/BTreeIndex.pxc");
        }
    }
}
