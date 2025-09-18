using System;
using System.Data.SqlClient; // direct DB usage
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExecuteNonQueryMethodExample
{
    // EF Core DbContext (Compliant ORM abstraction)
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ExecuteNonQueryTest
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public void Bad_Use_ExecuteNonQuery_Sync()
        {
            // ❌ Violation: ExecuteNonQuery() is synchronous and blocks the calling thread
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("INSERT INTO Products (Name) VALUES ('Laptop')", connection))
                {
                    int rows = command.ExecuteNonQuery(); // violation
                    Console.WriteLine($"Inserted {rows} row(s)");
                }
            }
        }

        public async Task Good_Use_ExecuteNonQuery_Async()
        {
            // ✅ Compliant: Use ExecuteNonQueryAsync() for non-blocking execution
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("UPDATE Products SET Name = 'Tablet' WHERE Id = 1", connection))
                {
                    int rows = await command.ExecuteNonQueryAsync(); // compliant
                    Console.WriteLine($"Updated {rows} row(s)");
                }
            }
        }

        public async Task Good_Use_EntityFramework_Async()
        {
            // ✅ Compliant: Use EF Core SaveChangesAsync
            using (var context = new AppDbContext())
            {
                var product = new Product { Name = "Phone" };
                context.Products.Add(product);
                await context.SaveChangesAsync(); // compliant
                Console.WriteLine("Inserted via EF Core");
            }
        }
    }
}
