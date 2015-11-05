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
        /// <summary>
        /// отладочный тест
        /// </summary>
        static void Testing()
        {
            string schemaPath = @"e:\my_documents\coding\_vsprojects\students\ormpolar\schema.xml";

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


        static void Main(string[] args)
        {
            Testing();

            Console.ReadKey();
        }
    }
}
