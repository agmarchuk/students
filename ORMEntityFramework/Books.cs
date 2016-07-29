using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ORMEntityFramework
{
    public class Books
    {
        public class Book
        {
            [Key]
            //[Column(Order = 1)]
            public int id { get; set; }

            //[Key]
            //[Column(Order = 2)]
            public string title { get; set; }
            public int pages { get; set; }
            //[Index]
            public int id_author { get; set; }
        }

        public class Author
        {
            public int id { get; set; }
            public string name { get; set; }
        }
    }
}
