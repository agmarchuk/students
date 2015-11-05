using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{
    public class UserContext: DbContext
    {
        public UserContext(string schemaPath)
            : base(schemaPath)
        {
            Books = new DbSet<Book>();
            Authors = new DbSet<Author>();
        }

        public DbSet<Book> Books;
        public DbSet<Author> Authors;

        public override void Dispose()
        {
            base.Dispose();
            Books.Dispose();
            Authors.Dispose();
        }
    }
}
