using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestPlatform;

namespace TestPlatform
{
    public class BookLibrary
    {
        public class Book
        {
            public virtual int id { get; set; }
            public virtual int Authorid { get; set; }//todo
            public virtual string title { get; set; }
            public virtual int pages { get; set; }
        }

        public class Author
        {
            public virtual int id { get; set; }
            public virtual string name { get; set; }
            public virtual ISet<Book> books { get; set; }
        }
    }
}
