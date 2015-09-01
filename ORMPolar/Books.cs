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
        public string title;
        public int pages;
        public int id_author;
    }

    public class Author
    {
        public int id;
        public string name;
    }
}
