using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using System.IO;
using ORMPolar;

namespace TestPlatform.UnitTests
{
    [TestClass]
    public class UnitTestORMPolar
    {
        [TestMethod]
        public void TestAppendElement()
        {
            string schemaPath = "";//@"e:\my_documents\coding\_vsprojects\students\ormpolar\schema.xml";

            using (UserContext uc = new UserContext(schemaPath))
            {
                uc.Books.Clear();

                Book book = new Book()
                {
                    Id = 1,
                    Title = "Война и мир",
                    Pages = 1001,
                    Id_author = 1
                };

                uc.Books.Append(book);
                uc.Books.Flush();

                var actual = uc.Books.Elements().First();

                Assert.AreEqual(book.Id, actual.Id);
                Assert.AreEqual(book.Id_author, actual.Id_author);
                Assert.AreEqual(book.Pages, actual.Pages);
                Assert.AreEqual(book.Title, actual.Title);
            }

            XDocument schema = XDocument.Load(schemaPath);

            foreach (XElement element in schema.Root.Elements()
                    .Where(el => el.Name == "class"))
            {
                string filePath = element.Attribute("path").Value;
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

        }

        [TestMethod]
        public void TestDeleteElement()
        {
            string schemaPath = "";//@"e:\my_documents\coding\_vsprojects\students\ormpolar\schema.xml";

            using (UserContext uc = new UserContext(schemaPath))
            {
                uc.Books.Clear();

                Book book = new Book()
                {
                    Id = 1,
                    Title = "Война и мир",
                    Pages = 1001,
                    Id_author = 1
                };

                uc.Books.Append(book);
                uc.Books.Flush();
                uc.Books.Delete(ref book);

                var actual = uc.Books.Elements().First();

                Assert.AreEqual(actual, null);
            }

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
