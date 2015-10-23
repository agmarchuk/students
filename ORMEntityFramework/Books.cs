using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMEntityFramework
{
    public class Books
    {
        public class Book
        {
            public int id { get; set; }
            public string title { get; set; }
            public int pages { get; set; }
            public int id_author { get; set; }
        }

        public class Author
        {
            public int id { get; set; }
            public string name { get; set; }
        }
    }
}
