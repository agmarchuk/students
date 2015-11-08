using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{
    public class Book
    {
        public int id;
        [Index("BTree")]
        public string title;
        public int pages;

        [Index("BTree")]
        [ForeignKey("Author")]
        public int id_author { get; set; }
    }

    public class Author
    {
        public int id;
        public string name;

        [OneToMany("AuthorId", "idAuthor")]
        public DbSet<Book> books { get;}
        //{ {get books.Where<TEntity>.idAuthor = AuthorId}; set; }
    }
}
