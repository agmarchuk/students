using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ORMEntityFramework;
using System.Data.Entity;

namespace TestPlatform.SpeedTests
{
    public class TestORMEntityFramework : IPerformanceTest
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        UserContext uc;

        public long CreateDB(int N)
        {
            sw.Reset();
            Random rnd = new Random();

            uc = new UserContext();

            for (int i = 0; i < N; ++i)
            {
                Books.Book book = new Books.Book()
                {
                    id = i,
                    title = "book" + i,
                    pages = 1001,
                    id_author = (rnd.Next(N) + rnd.Next(N)) % N
                };
                sw.Start();
                uc.Books.Add(book);
                sw.Stop();
            }
            uc.SaveChanges();

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public void DeleteDB()
        {
            uc.Database.Delete();
            uc.Dispose();
        }

        public long FindAll(int repeats, string fieldName)
        {
            sw.Reset();

            Random rnd = new Random();
            for (int i = 0; i < repeats; ++i)
            {
                int r = rnd.Next(1000000);
                List<Books.Book> books;
                if (fieldName == "title")
                {
                    sw.Start();
                    books = uc.Books.Where(b => b.title == "book" + r).ToList<Books.Book>();
                    sw.Stop();
                }
                else
                {
                    sw.Start();
                    books = uc.Books.Where(b => b.id_author == r).ToList<Books.Book>();
                    sw.Stop();
                }
            }
            
            return sw.ElapsedMilliseconds;
        }

        public long FindAll(string fieldName, object obj, out int count)
        {
            throw new NotImplementedException();
        }

        public long FindFirst(int repeats, string fieldName)
        {
            sw.Reset();

            Random rnd = new Random();
            for (int i = 0; i < repeats; ++i)
            {
                int r = rnd.Next(1000);
                Books.Book book;
                if (fieldName == "title")
                {
                    sw.Start();
                    var buf = uc.Books.Where(b => b.title == "book" + r);

                    sw.Stop();
                }
                else
                {
                    sw.Start();
                    uc.Books.Where(b => b.id_author == r);
                    sw.Stop();
                }

                //if (book != null)
                //    Console.WriteLine(book.title);
                //else
                //    Console.WriteLine("не найдена");
                }

            return sw.ElapsedMilliseconds;
            }

        public long FindFirst(string fieldName, object obj)
        {
            throw new NotImplementedException();
        }
    }
}
