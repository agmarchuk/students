using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.IO;
using System.Xml.Linq;

namespace ORMPolar
{
    public class Program
    {
        static string schemaPath = @"e:\my_documents\coding\_vsprojects\students\ormpolar\schema.xml";

        /// <summary>
        /// отладочный тест
        /// </summary>
        static void Testing()
        {
            
            using (UserContext uc = new UserContext(schemaPath))
            {
                uc.Books.Clear();

                Book book = new Book()
                {
                    //id = 1,
                    title = "Война и мир",
                    //pages = 1001,
                    id_author = 1
                };

                uc.Books.Append(book);
                book = new Book()
                {
                    //id = 2,
                    title = "Мастер йода и зеленки",
                    //pages = 10000,
                    id_author = 2
                };

                uc.Books.Append(book);

                uc.Books.Flush();
                
                foreach (var el in uc.Books.Elements())
                {
                    //Console.WriteLine("{0}  {1} {2}  {3}", el.id, el.title, el.pages, el.id_author);
                }

                //Проверим LINQ
                Console.WriteLine("\nКнига с id=1");
                //foreach (var el in uc.Books.Elements()
                    //.Where(boook => boook.id == 1))
                {
                    //Console.WriteLine("{0}  {1} {2}  {3}", el.id, el.title, el.pages, el.id_author);
                }
            }

            XDocument schema = XDocument.Load(schemaPath);

            foreach (XElement element in schema.Root.Elements()
                    .Where(el => el.Name == "class"))
            {
                string filePath = element.Attribute("path").Value;
                if (File.Exists(filePath))
                    File.Delete(filePath);

                //filePath = Path.GetDirectoryName(element.Attribute("path").Value);
                //if (File.Exists(filePath+ "/Index[books]-[title].pxc"))
                //    File.Delete(filePath + "/Index[books]-[title].pxc");
            }        

        }

        static void TestRelationOneToMany()
        {
            using (UserContext uc = new UserContext(schemaPath))
            {
                uc.Authors.Clear();
                uc.Books.Clear();

                Book book = new Book()
                {
                    id = 1,
                    title = "Евгений Онегин",
                    pages = 524,
                    id_author = 1
                };

                uc.Books.Append(book);
                book = new Book()
                {
                    id = 2,
                    title = "Руслан и Людмила",
                    pages = 100,
                    id_author = 1
                };

                uc.Books.Append(book);

                book = new Book()
                {
                    id = 3,
                    title = "Война и мир",
                    pages = 10000,
                    id_author = 2
                };

                uc.Books.Append(book);

                uc.Books.Flush();

                Author author = new Author()
                {
                    id = 1,
                    name = "Пушкин"
                };
                uc.Authors.Append(author);
                uc.Authors.Flush();

                Author a = uc.Authors.FindFirst("name", "Пушкин");

                DbSet<Book> authorBooks = a.books;

                foreach(Book b in authorBooks.Elements())
                {
                    Console.WriteLine("{0} {1} {2}",b.id, b.pages, b.title);
                }
            }

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

        static void Main(string[] args)
        {
            TestRelationOneToMany();

            Console.ReadKey();
        }
    }
}
