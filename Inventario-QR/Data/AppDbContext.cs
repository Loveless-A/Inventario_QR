using Microsoft.EntityFrameworkCore;
using Inventario_QR.Models;

namespace Inventario_QR.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Person> Persons { get; set; }
        public DbSet<Access> Accesses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<Detail> Details { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
    }
}