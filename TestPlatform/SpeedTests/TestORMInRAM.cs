﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using ORMPolar;

//namespace TestPlatform.SpeedTests
//{
//    class TestDBInRAM:IPerformanceTest
//    {
//        private List<Book> books;
//        private Dictionary<string, Book> index_title;
//        private Dictionary<int, List<Book>> index_id_author;

//        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

//        Random rnd = new Random();

//        public long CreateDB(int N)
//        {
//            sw.Reset();
//            books = new List<Book>();
//            index_title = new Dictionary<string, Book>();
//            index_id_author = new Dictionary<int, List<Book>>();

//            for (int i = 0; i < N; ++i)
//            {
//                List<Book> arrayBook;
//                Book book = new Book()
//                {
//                    id = i,
//                    title = "book" + i,
//                    pages = 1001,
//                    id_author = (rnd.Next(N) + rnd.Next(N)) % N
//                };
//                sw.Start();
//                books.Add(book);

//                index_title.Add(book.title, book);

//                if (index_id_author.TryGetValue(book.id_author, out arrayBook))
//                    arrayBook.Add(book);
//                else
//                {
//                    arrayBook = new List<Book>();
//                    arrayBook.Add(book);
//                    index_id_author.Add(book.id_author, arrayBook);
//                }

//                sw.Stop();
//            }
//            return sw.ElapsedMilliseconds;
//        }

//        public void DeleteDB()
//        {
//            System.GC.Collect();
//        }

//        public long FindFirst(int repeats, string fieldName)
//        {
//            sw.Reset();

//            Random rnd = new Random();
//            int N = books.Count();

//            for (int i = 0; i < (repeats * repeats); ++i)
//            {
//                int r = (rnd.Next(N) + rnd.Next(N)) % N;
//                Book book;
//                List<Book> books;
//                if (fieldName == "title")
//                {
//                    sw.Start();
//                    index_title.TryGetValue("book" + r, out book);
//                    sw.Stop();
//                }
//                else
//                {
//                    sw.Start();
//                    index_id_author.TryGetValue(r, out books);
//                    book = books?.First();
//                    sw.Stop();
//                }

//                //if (book != null)
//                //    Console.WriteLine(book.title);
//                //else
//                //    Console.WriteLine("не найдена");
//            }

//            return sw.ElapsedMilliseconds;
//        }

//        public long FindAll(int repeats, string fieldName)
//        {
//            sw.Reset();
//            //count = 0;
//            int N = books.Count();
//            Random rnd = new Random();
//            for (int i = 0; i < (repeats * repeats); ++i)
//            {
//                int r = (rnd.Next(N) + rnd.Next(N)) % N;
//                Book book;
//                List<Book> books;
//                if (fieldName == "title")
//                {
//                    sw.Start();
//                    index_title.TryGetValue("book" + r, out book);
//                    sw.Stop();
//                    //count = (book == null) ? 0 : 1; 
//                }
//                else
//                {
//                    sw.Start();
//                    index_id_author.TryGetValue(r, out books);
//                    sw.Stop();
                    
//                    //count = (books == null) ? 0 : books.Count();
//                }
                
//                //Console.WriteLine(count);
//            }

//            return sw.ElapsedMilliseconds;
//        }

//        public long WarmUp()
//        {
//            sw.Reset();
//            int N = books.Count();
//            sw.Start();
//                foreach (var book in books) { }
//                foreach (var index in index_id_author.Values) { }
//                foreach (var index in index_title.Values) { }
//            sw.Stop();
//            return sw.ElapsedMilliseconds;
//        }
//    }
//}
