using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{
    class UserContext: DbContext
    {
        //TODO: Реализация синглтона на совести разработчика
        public UserContext(string schemaPath)
            : base(schemaPath)
        {
            Books = new DbSet<Book>();
            Authors = new DbSet<Author>();
        }

        public DbSet<Book> Books;
        public DbSet<Author> Authors;

       
    }
}
