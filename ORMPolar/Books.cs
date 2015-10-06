using System;
using System.Collections.Generic;
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
        public int id_author;
    }

    public class Author
    {
        //[IndexArray]
        public int id;
        public string name;
    }
}
