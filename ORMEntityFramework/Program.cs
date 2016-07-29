using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMEntityFramework
{
    public class Program
    {
        public class TestORMEntityFramework
        {
            NorthWind.Customer GetCustomer(int Id)
            {
                return new NorthWind.Customer()
                {
                    Id = Id,
                    Name = String.Format("Customer{0}", Id)
                };
            }

            NorthWind.Order GetOrder(int Id, NorthWind.Customer Customer)
            {
                return new NorthWind.Order()
                {
                    Id = Id,
                    CustomerId = Customer.Id,
                    Customer = Customer,
                    Title = String.Format("Title{0}", Id)
                };
            }

            NorthWind.OrderItem GetOrderItem(int Id, NorthWind.Order Order, NorthWind.Product Product)
            {
                return new NorthWind.OrderItem()
                {
                    Id = Id,
                    OrderId = Order.Id,
                    Order = Order,
                    ProductId = Product.Id,
                    Product = Product,
                    Quantity = (rnd.Next(1000)) % 1000
                };
            }

            NorthWind.Product GetProduct(int Id)
            {
                return new NorthWind.Product()
                {
                    Id = Id,
                    Name = String.Format("Product{0}", Id),
                    Price = (rnd.Next(10000)) % 10000,
                };
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            UserContext uc;
            Random rnd = new Random();
            int i;

            public void Init()
            {
                int i = 0;
                uc = new UserContext();

            }

            public long Add(int countPortions, int sizePortion)
            {
                sw.Reset();
                List<NorthWind.Customer> _customers = new List<NorthWind.Customer>();
                List<NorthWind.Order> _orders = new List<NorthWind.Order>();
                List<NorthWind.OrderItem> _orderItems = new List<NorthWind.OrderItem>();
                List<NorthWind.Product> _products = new List<NorthWind.Product>();

                NorthWind.Customer customer;
                NorthWind.Order order;
                NorthWind.OrderItem orderItem;
                NorthWind.Product product;

                for (int j = 0; j < countPortions; ++j)
                {

                    for (int k = 0; k < sizePortion; ++k)
                    {
                        customer = GetCustomer(i);
                        product = GetProduct(i);
                        order = GetOrder(i, customer);
                        orderItem = GetOrderItem(i, order, product);
                        _customers.Add(customer);
                        _products.Add(product);
                        _orders.Add(order);
                        _orderItems.Add(orderItem);
                        ++i;
                    }

                    sw.Start();
                    uc.Customers.AddRange(_customers);
                    uc.SaveChanges();
                    uc.Products.AddRange(_products);
                    uc.SaveChanges();
                    uc.Orders.AddRange(_orders);
                    uc.SaveChanges();
                    uc.OrderItems.AddRange(_orderItems);
                    uc.SaveChanges();
                    sw.Stop();

                    _customers.Clear();
                    _products.Clear();
                    _orders.Clear();
                    _orderItems.Clear();

                    Console.WriteLine(i + ") " + sw.ElapsedMilliseconds);
                }
                return sw.ElapsedMilliseconds;
            }
            //int portions = N / 100000;
            //int k = 0;
            //for (; i < portions; ++i)
            //{
            //    List<Books.Book> books = new List<Books.Book>();
            //    for (int j = 0; j < 100000; ++j)
            //    {
            //        Books.Book book = new Books.Book()
            //        {
            //            id = k,
            //            title = "book" + k,
            //            pages = 1001,
            //            id_author = (rnd.Next(N) + rnd.Next(N)) % N
            //        };
            //        books.Add(book);
            //        ++k;
            //    }

            //    sw.Start();
            //    uc.Books.AddRange(books);
            //    uc.SaveChanges();
            //    sw.Stop();
            //    Console.WriteLine(i+") "+sw.ElapsedMilliseconds);
            //}

            //    return sw.ElapsedMilliseconds;
            //}

            public void DeleteDB()
            {
                uc.Database.Delete();
                uc.Dispose();
            }

            //    public long FindAll(int repeats, string fieldName)
            //    {
            //        sw.Reset();

            //        Random rnd = new Random();
            //        for (int i = 0; i < repeats; ++i)
            //        {
            //            int r = rnd.Next(uc.Books.Count());
            //            List<Books.Book> books;
            //            if (fieldName == "title")
            //            {
            //                sw.Start();
            //                books = uc.Books.Where(b => b.title == "book" + r).ToList<Books.Book>();
            //                sw.Stop();
            //            }
            //            else
            //            {
            //                sw.Start();
            //                books = uc.Books.Where(b => b.id_author == r).ToList<Books.Book>();
            //                sw.Stop();
            //            }
            //        }

            //        return sw.ElapsedMilliseconds;
            //    }

            //    public long FindFirst(int repeats, string fieldName)
            //    {
            //        sw.Reset();

            //        Random rnd = new Random();
            //        for (int i = 0; i < repeats; ++i)
            //        {
            //            int r = rnd.Next(uc.Books.Count());
            //            Books.Book book;
            //            if (fieldName == "title")
            //            {
            //                sw.Start();
            //                try
            //                {
            //                    book = uc.Books.First(b => b.title == "book" + r);
            //                }
            //                catch (Exception)
            //                {
            //                    //книга не найдена 
            //                }
            //                sw.Stop();
            //            }
            //            else
            //            {
            //                sw.Start();
            //                try
            //                {
            //                    book = uc.Books.First(b => b.id_author == r);

            //                }
            //                catch (Exception)
            //                {
            //                    //книга не найдена 
            //                }
            //                sw.Stop();
            //            }
            //        }

            //        return sw.ElapsedMilliseconds;
            //    }

            //    public long WarmUp()
            //    {
            //        return 0L;
            //    }
            //}
        }
        public static void Main()
        {
            Console.WriteLine(Properties.Settings.Default.DbConnection);
            var test = new TestORMEntityFramework();
            test.Init();
            var t = test.Add(1, 100000);
            Console.WriteLine("final= "+t);
            test.DeleteDB();
            Console.ReadKey();
        }
    }
}
