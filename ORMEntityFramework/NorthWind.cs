using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ORMEntityFramework
{
    public class NorthWind
    {

        public class Customer 
        {
            [Key]
            public int Id { get; set; }
            public string Name { get; set; }

            public ICollection<Order> Orders { get; set; }
            public Customer()
            {
                Orders = new List<Order>();
            }
        }

        public class Order 
        {
            [Key]
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public string Title { get; set; }

            public virtual Customer Customer { get; set; }
            public ICollection<OrderItem> OrderItems { get; set; }

            public Order()
            {
                OrderItems = new List<OrderItem>();
            }
        }

        public class OrderItem 
        {
            [Key]
            public int Id { get; set; }
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }

            public virtual Order Order { get; set; }
            public virtual Product Product { get; set; }
        }

        public class Product
        {
            [Key]
            public int Id { get; set; }
            public string Name { get; set; }
            public int Price { get; set; }
        }
    }
}
