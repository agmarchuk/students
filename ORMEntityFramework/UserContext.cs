using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace ORMEntityFramework
{
    public class UserContext : DbContext
    {
        public UserContext() 
            : base(Properties.Settings.Default.DbConnection) { }
        //"Data Source=(localdb)\v11.0;Initial Catalog=NorthWind;Integrated Security=True;Connect Timeout=3;Encrypt=False;TrustServerCertificate=False")//
        //public DbSet<Books.Book> Books { get; set; }
        public DbSet<NorthWind.Customer> Customers { get; set; }
        public DbSet<NorthWind.Order> Orders { get; set; }
        public DbSet<NorthWind.OrderItem> OrderItems { get; set; }
        public DbSet<NorthWind.Product> Products { get; set; }
    }
}
