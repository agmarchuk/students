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
        static string schemaPath = AppDomain.CurrentDomain.BaseDirectory + @"..\..\schema.xml";

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
                    Title = "Война и мир",
                    //pages = 1001,
                    Id_author = 1
                };

                uc.Books.Append(book);
                book = new Book()
                {
                    //id = 2,
                    Title = "Мастер йода и зеленки",
                    //pages = 10000,
                    Id_author = 2
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
                    Id = 1,
                    Title = "Евгений Онегин",
                    Pages = 524,
                    Id_author = 1
                };

                uc.Books.Append(book);
                book = new Book()
                {
                    Id = 2,
                    Title = "Руслан и Людмила",
                    Pages = 100,
                    Id_author = 1
                };

                uc.Books.Append(book);

                book = new Book()
                {
                    Id = 3,
                    Title = "Война и мир",
                    Pages = 10000,
                    Id_author = 2
                };

                uc.Books.Append(book);

                uc.Books.Flush();

                Author author = new Author()
                {
                    Id = 1,
                    Name = "Пушкин"
                };
                uc.Authors.Append(author);
                uc.Authors.Flush();

                Author a = uc.Authors.FindFirst("Name", "Пушкин");

                //DbSet<Book> authorBooks = a.books;

                //foreach(Book b in authorBooks.Elements())
                //{
                //    Console.WriteLine("{0} {1} {2}",b.Id, b.Pages, b.Title);
                //}
            }

            XDocument schema = XDocument.Load(schemaPath);

            foreach (XElement element in schema.Root.Elements()
                    .Where(el => el.Name == "class"))
            {
                string filePath = element.Attribute("path").Value;
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            if (File.Exists("Index[books]-[Title].pxc"))
                File.Delete("Index[books]-[Title].pxc");
            if (File.Exists("Index[books]-[Id_author].pxc"))
                File.Delete("Index[books]-[Id_author].pxc");

            if (File.Exists("../../../Databases" + "/Index[books]-[Title].pxc"))
                File.Delete("../../../Databases" + "/Index[books]-[Title].pxc");
            if (File.Exists("../../../Databases" + "/Index[books]-[Id_author].pxc"))
                File.Delete("../../../Databases" + "/Index[books]-[Id_author].pxc");

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
