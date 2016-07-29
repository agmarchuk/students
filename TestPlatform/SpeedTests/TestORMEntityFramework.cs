using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using ORMEntityFramework;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestPlatform.SpeedTests
{
    public class TestORMEntityFramework //: IPerformanceTest
    {
        public class Books
        {
            public class Book
            {
                [Key]
                [Column(Order = 1)]
                public int id { get; set; }

                [Key]
                [Column(Order = 2)]
                public string title { get; set; }
                public int pages { get; set; }
                [Index]
                public int id_author { get; set; }
            }

            public class Author
            {
                public int id { get; set; }
                public string name { get; set; }
            }
        }

        public class UserContext : DbContext
        {
            public UserContext() : base("DefaultConnection") { } //Properties.Settings.Default.DbConnection

            public DbSet<Books.Book> Books { get; set; }
        }

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        UserContext uc;

        public long CreateDB(int N)
        {
            sw.Reset();
            Random rnd = new Random();

            uc = new UserContext();
            int portions = N / 1000;
            int k = 0;
            for (int i = 0; i < portions; ++i)
            {
                List<Books.Book> books=new List<Books.Book>();
                for (int j = 0; j < 1000; ++j)
                {
                    Books.Book book = new Books.Book()
                    {
                        id = k,
                        title = "book" + k,
                        pages = 1001,
                        id_author = (rnd.Next(N) + rnd.Next(N)) % N
                    };
                    books.Add(book);
                    ++k;
                }

                sw.Start();
                uc.Books.AddRange(books);
                uc.SaveChanges();
                sw.Stop();
                //Console.WriteLine(i+") "+sw.ElapsedMilliseconds);
            }

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
                int r = rnd.Next(uc.Books.Count());
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

        public long FindFirst(int repeats, string fieldName)
        {
            sw.Reset();

            Random rnd = new Random();
            for (int i = 0; i < repeats; ++i)
            {
                int r = rnd.Next(uc.Books.Count());
                Books.Book book;
                if (fieldName == "title")
                {
                    sw.Start();
                    try
                    {
                        book = uc.Books.First(b => b.title == "book" + r);
                    }
                    catch (Exception) 
                    {
                        //книга не найдена 
                    } 
                    sw.Stop();
                }
                else
                {
                    sw.Start();
                    try
                    {
                        book = uc.Books.First(b => b.id_author == r);
                        
                    }
                    catch (Exception) 
                    {
                        //книга не найдена 
                    } 
                    sw.Stop();
                }
            }

            return sw.ElapsedMilliseconds;
            }

        public long WarmUp()
        {
            return 0L;
        }
    }
}
