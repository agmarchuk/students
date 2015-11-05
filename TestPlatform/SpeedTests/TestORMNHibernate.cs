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

namespace TestPlatform.SpeedTests
{
    public class TestORMNHibernate:IPerformanceTest
    {
        private ISession session;

        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private Random rnd = new Random();

        private ISession OpenSession()
        {
            var configuration = new Configuration().Configure("Mappings/hibernate.cfg.xml");
            configuration.AddAssembly(typeof(BookLibrary).Assembly);
            ISessionFactory sessionFactory = configuration.BuildSessionFactory();
            new SchemaUpdate(configuration).Execute(true, true);
            return sessionFactory.OpenSession();
        }

        public TestORMNHibernate()
        {
            try
            {
                session = OpenSession();
            }
            catch (Exception ex) { Console.WriteLine("Ошибка: " + ex.Message); }
        }

        public long CreateDB(int N)
        {
            sw.Reset();
            using (ITransaction transaction = session.BeginTransaction())
            {
                for (int i = 0; i < N; ++i)
                {
                    BookLibrary.Book book = new BookLibrary.Book()
                    {
                        id = i,
                        title = "book" + i,
                        pages = (rnd.Next(N)+rnd.Next(N)) % N,
                    };

                    sw.Start();

                    session.Save(book);

                    sw.Stop();
                }
                transaction.Commit();
            }

            return sw.ElapsedMilliseconds;
        }
        public int GetCountBooks()
        {
            ICriteria criteria = session.CreateCriteria(typeof(BookLibrary.Book));

            var books = criteria.List<BookLibrary.Book>();
            return books.Count();
        }

        public void DeleteDB()
        {
            session.Close();
        }

        public long FindAll(int repeats, string fieldName)
        {
            sw.Reset();
            int r = rnd.Next(1000000);
            ICriteria criteria = session.CreateCriteria(typeof(BookLibrary.Book));

            if (fieldName == "title")
            {
                sw.Start();
                IList<BookLibrary.Book> list = criteria.Add(Restrictions.Like("title", "book" + r)).List<BookLibrary.Book>();
                sw.Stop();
            }
            else
            {
                sw.Start();
                IList<BookLibrary.Book> list = criteria.Add(Restrictions.Eq("pages",r)).List<BookLibrary.Book>();
                sw.Stop();
            }
            return sw.ElapsedMilliseconds;
        }

        public long FindFirst(int repeats, string fieldName)
        {
            sw.Reset();
            int r = rnd.Next(1000000);
            ICriteria criteria = session.CreateCriteria(typeof(BookLibrary.Book));

            if (fieldName == "title")
            {
                sw.Start();
                IList<BookLibrary.Book> list = criteria.Add(Restrictions.Like("title", "book" + r)).List<BookLibrary.Book>();
                if (list!=null && list.Count()!=0) list.First();
                sw.Stop();
            }
            else
            {
                sw.Start();
                IList<BookLibrary.Book> list = criteria.Add(Restrictions.Eq("pages", r)).List<BookLibrary.Book>();
                if (list != null && list.Count()!=0) list.First();
                sw.Stop();
            }
            return sw.ElapsedMilliseconds;
        }

        public long WarmUp()
        {
            return 0L;
        }
    }
}
