using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace ORMPolar
{
    class Program
    {
        static void Test1()
        {
            UserContext uc = new UserContext(@"e:\my_documents\coding\_vsprojects\students\ormpolar\schema.xml");
            Book book = new Book()
            {
                id = 1,
                title = "Война и мир",
                pages = 1001,
                id_author = 1
            };
            uc.Books.Clear();
            uc.Books.Append(book);
            uc.Books.Flush();

            var res = uc.Books.Get().Root.GetValue();
            Console.WriteLine(res.Type.Interpret(res.Value));
        }
        static void Main(string[] args)
        {
            Test1();

            Console.ReadKey();
        }
    }
}
