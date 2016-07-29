using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPlatform
{
    public class NorthWind
    {
        public class Customer
        {
            public virtual int Id { get; set; }
            public virtual string Name { get; set; }
            public virtual IList<Order> Orders { get; set; }
        }

        public class Order
        {
            public virtual int Id { get; set; }
            public virtual Customer Customer { get; set; }//
            public virtual int CustomerId { get; set; }
            public virtual string Title { get; set; }
            public virtual IList<OrderItem> OrderItems { get; set; }
        }

        public class OrderItem
        {
            public virtual int Id { get; set; }
            public virtual Order Order { get; set; }//
            public virtual int OrderId { get; set; }
            public virtual Product Product { get; set; }//
            public virtual int ProductId { get; set; }
            public virtual int Quantity { get; set; }
        }

        public class Product
        {
            public virtual string Name { get; set; }
            public virtual int Id { get; set; }
            public virtual int Price { get; set; }
        }

    }
}
