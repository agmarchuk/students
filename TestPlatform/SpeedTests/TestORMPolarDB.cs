using System;
using ORMPolar;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace TestPlatform.SpeedTests
{
    public class TestORMPolarDB : IPerformanceTest
    {
       

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        Random rnd = new Random();
        string schemaPath = @"../../NorthWindSchema.xml";
        NorthWindContext nwc;

        public void Init()
        {
            nwc = new NorthWindContext(schemaPath);
            nwc.Customers.Clear();
            nwc.Orders.Clear();
            nwc.OrderItems.Clear();
            nwc.Products.Clear();
            i = 0;
        }

        Customer GetCustomer(int Id)
        {
            return new Customer()
            {
                Id = Id,
                Name = String.Format("Customer{0}", Id)
            };
        }

        Order GetOrder(int Id, int CustomerId)
        {
            return new Order()
            {
                Id = Id,
                CustomerId = CustomerId,
                Title = String.Format("Title{0}", Id)
            };
        }

        OrderItem GetOrderItem(int Id, int OrderId, int ProductId)
        {
            return new OrderItem()
            {
                Id = Id,
                OrderId = OrderId,
                ProductId = ProductId,
                Quantity = (rnd.Next(1000)) % 1000
            };
        }

        Product GetProduct(int Id)
        {
            return new Product()
            {
                Id = Id,
                Name = String.Format("Product{0}", Id),
                Price = (rnd.Next(10000)) % 10000,
            };
        }


        int i;
        public long Add(int N)
        {
            sw.Reset();
            int k = i + N;
            for (; i < k; ++i)
            {
                var customer = GetCustomer(i);
                var order = GetOrder(i, i);
                var orderItem = GetOrderItem(i, i, i);
                var product = GetProduct(i);

                sw.Start();
                nwc.Customers.Append(customer);
                nwc.Customers.Flush();
                nwc.Products.Append(product);
                nwc.Products.Flush();
                nwc.Orders.Append(order);
                nwc.Orders.Flush();
                nwc.OrderItems.Append(orderItem);
                nwc.OrderItems.Flush();
                sw.Stop();
            }

            return sw.ElapsedMilliseconds;
        }

        public long FindString(int repeats)
        {
            string fieldName = "Name";
            sw.Reset();
            for (int j = 0; j < repeats; ++j)
            {
                string Name = String.Format("Product{0}", (rnd.Next(i)) % i);
                
                sw.Start();
                var product = nwc.Products.FindFirst(fieldName, Name);
                sw.Stop();
                //Console.WriteLine("product " + product.Name + " will found");
            }
            return sw.ElapsedMilliseconds;
        }

        public long FindInt(int repeats)
        {
            string fieldName = "Id";
            sw.Reset();
            for (int j = 0; j < repeats; ++j)
            {
                int Id = (rnd.Next(i)) % i;
                
                sw.Start();
                var product = nwc.Products.FindFirst(fieldName, Id);
                sw.Stop();
                //Console.WriteLine("product " + product.Name + " will found");
            }
            return sw.ElapsedMilliseconds;
        }

        public long FindFirst(int repeats, string fieldName)
        {
            sw.Reset();
            //Random rnd = new Random();
            //int N = nwc.Books.Elements().Count();

            //for (int i = 0; i < repeats; ++i)
            //{
            //    int r = rnd.Next(N);
            //    Book book;
            //    if (fieldName == "title")
            //    {
            //        sw.Start();
            //        book = nwc.Books.FindFirst(fieldName, (object)String.Concat("book", r));
            //        sw.Stop();
            //    }
            //    else
            //    {
            //        sw.Start();
            //        book = nwc.Books.FindFirst(fieldName, (object)r);
            //        sw.Stop();
            //    }


            //    //if (book != null)
            //    //    Console.WriteLine(book.title);
            //    //else
            //    //    Console.WriteLine("не найдена");
            //}

            return sw.ElapsedMilliseconds;
        }

        public long FindAll(int repeats, string fieldName)
        {
            sw.Reset();

            //Random rnd = new Random();
            //int N = nwc.Books.Elements().Count();

            //for (int i = 0; i < repeats; ++i)
            //{
            //    int r = rnd.Next(N);
            //    List<Book> books;
            //    if (fieldName == "title")
            //    {
            //        sw.Start();
            //        books = nwc.Books.FindAll(fieldName, (object)String.Concat("book", r)).ToList<Book>();
            //        sw.Stop();
            //    }
            //    else
            //    {
            //        sw.Start();
            //        books = nwc.Books.FindAll(fieldName, (object)r).ToList<Book>();
            //        sw.Stop();
            //    }
            //}

            return sw.ElapsedMilliseconds;
        }

        public void DeleteDB()
        {
            nwc.Dispose();

            //XDocument schema = XDocument.Load(schemaPath);

            //foreach (XElement element in schema.Root.Elements()
            //        .Where(el => el.Name == "class"))
            //{
            //    string filePath = element.Attribute("path").Value;
            //    if (File.Exists(filePath))
            //        File.Delete(filePath);
            //}

            //if (File.Exists("Index[books]-[Title].pxc"))
            //    File.Delete("Index[books]-[Title].pxc");
            //if (File.Exists("Index[books]-[Id_author].pxc"))
            //    File.Delete("Index[books]-[Id_author].pxc");

            //if (File.Exists("../../../Databases" + "/Index[books]-[Title].pxc"))
            //    File.Delete("../../../Databases" + "/Index[books]-[Title].pxc");
            //if (File.Exists("../../../Databases" + "/Index[books]-[Id_author].pxc"))
            //    File.Delete("../../../Databases" + "/Index[books]-[Id_author].pxc");

            //if (File.Exists("../../../Databases" + "/BTreeIndex.pxc"))
            //    File.Delete("../../../Databases" + "/BTreeIndex.pxc");
        }

        //public long WarmUp()
        //{
        //    sw.Reset();
        //    Random rnd = new Random();
        //    int N = nwc.Books.Elements().Count();
        //    List<Book> books;

        //    foreach (var book in nwc.Books.Elements()) { }

        //    for (int i = 0; i < 1000; ++i)
        //    {
        //        int r = (rnd.Next(N) + rnd.Next(N)) % N;
        //        sw.Start();
        //        books = nwc.Books.FindAll("Id_author", (object)r).ToList<Book>();
        //        books = nwc.Books.FindAll("Title", (object)(String.Concat("book", r))).ToList<Book>();
        //        sw.Stop();
        //    }
        //    return sw.ElapsedMilliseconds;
        //}
    }
}
