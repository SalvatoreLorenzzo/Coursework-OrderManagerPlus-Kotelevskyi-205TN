using Microsoft.EntityFrameworkCore;

namespace OrderManagerPlus.Models
{
    public class Context : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var settings = AppSettings.Load();
            var dbPath = settings.DatabasePath;

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}