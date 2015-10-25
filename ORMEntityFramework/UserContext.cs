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
        public UserContext() : //base("DBConnection")
                               //base(@"data source=(localdb)\ProjectsV12;AttachDbFilename=E:\My_Documents\Coding\_VSprojects\students\Databases\BookStoreEF.mdf;Integrated Security=True;Connect Timeout=30")
            base(@"data source=MAKC-PC\SQLEXPRESS;AttachDbFilename=D:\My_Documents\Coding\_VSprojects\students\Databases\BookStoreEF.mdf;Integrated Security=True;Connect Timeout=30")
        {

        }

        public DbSet<Books.Book> Books { get; set; }
    }
}
