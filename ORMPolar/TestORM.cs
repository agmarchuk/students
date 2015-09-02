using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using System.IO;

namespace ORMPolar
{
    [TestClass]
    public class TestORM
    {
        [TestMethod]
        public void TestAppendElement()
        {
            string schemaPath = @"e:\my_documents\coding\_vsprojects\students\ormpolar\schema.xml";

            using (UserContext uc = new UserContext(schemaPath))
            {
                uc.Books.Clear();

                Book book = new Book()
                {
                    id = 1,
                    title = "Война и мир",
                    pages = 1001,
                    id_author = 1
                };

                uc.Books.Append(book);
                uc.Books.Flush();

                var actual = uc.Books.Elements().First();

                Assert.AreEqual(book.id, actual.id);
                Assert.AreEqual(book.id_author, actual.id_author);
                Assert.AreEqual(book.pages, actual.pages);
                Assert.AreEqual(book.title, actual.title);
            }
            
            //TODO:После выполнения теста

            XDocument schema = XDocument.Load(schemaPath);

            foreach (XElement element in schema.Root.Elements()
                    .Where(el => el.Name == "class"))
            {
                string filePath = element.Attribute("path").Value;
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }        

        }
    }
}
