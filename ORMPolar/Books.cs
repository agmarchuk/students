using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{
   

    public class Book: Entity
    {
        [Index("BTree")]
        public string Title { get; set; }

        [Index("BTree")]
        //[ForeignKey("Author")]
        public int Id_author { get; set; }
        public int Id { get; set; }
        public int Pages { get; set; }
    }

    public class Author: Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        //[OneToMany("AuthorId", "idAuthor")]
        //public DbSet<Book> books { get;}
    }
}
