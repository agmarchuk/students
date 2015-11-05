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
        public UserContext() : base(Properties.Settings.Default.DbConnection) {}

        public DbSet<Books.Book> Books { get; set; }
    }
}
