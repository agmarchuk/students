using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate;
using System.Web;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Criterion;
using System.Collections;
using NHibernate.Mapping.Attributes;

namespace TestPlatform.SpeedTests
{
    public class TestORMNHibernate:IPerformanceTest
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
                Customer = Customer,
                //CustomerId = Customer.Id,
                Title = String.Format("Title{0}", Id)
            };
        }

        NorthWind.OrderItem GetOrderItem(int Id, NorthWind.Order Order, NorthWind.Product Product)
        {
            return new NorthWind.OrderItem()
            {
                Id = Id,
                ////-----------
                //OrderId = Order.Id,
                //ProductId = Product.Id,
                ////------------
                Order = Order,
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

        private ISession session;

        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private Random rnd = new Random();
        private Configuration configuration;
        private ISession OpenSession()
        {
            configuration = new Configuration().Configure("Mappings/hibernate.cfg.xml");
            var assembly = typeof(NorthWind).Assembly;
            configuration.AddAssembly(assembly);
            ISessionFactory sessionFactory = configuration.BuildSessionFactory();
            new SchemaUpdate(configuration).Execute(true, true);
            return sessionFactory.OpenSession();
        }



        public TestORMNHibernate()
        {
            
        }

        public void Init() 
        {
            i = 0;
            session = OpenSession();

        }

        int i = 0;

        public long Add(int N)
        {
            sw.Reset();
            using (ITransaction transaction = session.BeginTransaction())
            {
                for (; i < N; ++i)
                {
                    var customer = GetCustomer(i);
                    var product = GetProduct(i);
                    var order = GetOrder(i, customer);
                    var orderItem = GetOrderItem(i, order, product);

                    sw.Start();
                    session.Save(customer);
                    session.Save(product);
                    session.Save(order);
                    session.Save(orderItem);
                    sw.Stop();
                }
                transaction.Commit();
            }

            return sw.ElapsedMilliseconds;
        }
        //public int GetCountBooks()
        //{
        //    ICriteria criteria = session.CreateCriteria(typeof(BookLibrary.Book));

        //    var books = criteria.List<BookLibrary.Book>();
        //    return books.Count();
        //}

        public void DeleteDB()
        {
            session.Delete("from Object");
            //configuration.CreateMapping(NHibernate.Dialect.Dialect.GetDialect(configuration.Properties)).AddTable(catalog, schema, tablename, null, false, "drop");
            session.Close();
        }

        //public long FindAll(int repeats, string fieldName)
        //{
        //    sw.Reset();
        //    int r = rnd.Next(1000000);
        //    ICriteria criteria = session.CreateCriteria(typeof(BookLibrary.Book));

        //    if (fieldName == "title")
        //    {
        //        sw.Start();
        //        IList<BookLibrary.Book> list = criteria.Add(Restrictions.Like("title", "book" + r)).List<BookLibrary.Book>();
        //        sw.Stop();
        //    }
        //    else
        //    {
        //        sw.Start();
        //        IList<BookLibrary.Book> list = criteria.Add(Restrictions.Eq("pages",r)).List<BookLibrary.Book>();
        //        sw.Stop();
        //    }
        //    return sw.ElapsedMilliseconds;
        //}

        //public long FindFirst(int repeats, string fieldName)
        //{
        //    sw.Reset();
        //    int r = rnd.Next(1000000);
        //    ICriteria criteria = session.CreateCriteria(typeof(BookLibrary.Book));

        //    if (fieldName == "title")
        //    {
        //        sw.Start();
        //        IList<BookLibrary.Book> list = criteria.Add(Restrictions.Like("title", "book" + r)).List<BookLibrary.Book>();
        //        if (list!=null && list.Count()!=0) list.First();
        //        sw.Stop();
        //    }
        //    else
        //    {
        //        sw.Start();
        //        IList<BookLibrary.Book> list = criteria.Add(Restrictions.Eq("pages", r)).List<BookLibrary.Book>();
        //        if (list != null && list.Count()!=0) list.First();
        //        sw.Stop();
        //    }
        //    return sw.ElapsedMilliseconds;
        //}

        //public long WarmUp()
        //{
        //    return 0L;
        //}


        public long FindString(int repeats)
        {
            throw new NotImplementedException();
        }

        public long FindInt(int repeats)
        {
            throw new NotImplementedException();
        }
    }
}
