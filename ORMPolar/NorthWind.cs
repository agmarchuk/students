using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{


    public class Customer : Entity
    {
        [Index("BTree"), OneToMany("Order", "CustomerId")]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Order : Entity
    {
        [Index("BTree"), OneToMany("OrderItem", "OrderId")]
        public int Id { get; set; }
        [Index("BTree"), ManyToOne("Customer", "Id")]
        public int CustomerId { get; set; }
        public string Title { get; set; }
    }

    public class OrderItem : Entity
    {
        [Index("BTree")]
        public int Id { get; set; }
        [Index("BTree"), ManyToOne("Order", "Id")]
        public int OrderId { get; set; }
        [Index("BTree"), ManyToOne("Product", "Id")]
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class Product : Entity
    {
        [Index("BTree")]
        public string Name { get; set; }
        [Index("BTree"), OneToMany("OrderItem", "ProductId")]
        public int Id { get; set; }
        public int Price { get; set; }
    }


    public class NorthWindContext : DbContext
    {
        public NorthWindContext(string schemaPath)
            : base(schemaPath)
        {
            Customers = new DbSet<Customer>();
            Orders = new DbSet<Order>();
            OrderItems = new DbSet<OrderItem>();
            Products = new DbSet<Product>();
        }

        public DbSet<Customer> Customers;
        public DbSet<Order> Orders;
        public DbSet<OrderItem> OrderItems;
        public DbSet<Product> Products;

        public override void Dispose()
        {
            base.Dispose();
            Customers.Dispose();
            Orders.Dispose();
            OrderItems.Dispose();
            Products.Dispose();
        }
    }
    
}
