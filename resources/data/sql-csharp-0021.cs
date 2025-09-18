using System;
using System.Data.SqlClient; // direct DB usage
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExecuteScalarMethodExample
{
    // EF Core DbContext (Compliant ORM abstraction)
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ExecuteScalarTest
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public void Bad_Use_ExecuteScalar_Sync()
        {
            // ❌ Violation: ExecuteScalar() is synchronous and blocks thread
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT COUNT(*) FROM Customers", connection))
                {
                    int count = (int)command.ExecuteScalar(); // violation
                    Console.WriteLine($"Total customers: {count}");
                }
            }
        }

        public async Task Good_Use_ExecuteScalar_Async()
        {
            // ✅ Compliant: Use ExecuteScalarAsync() for non-blocking execution
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT COUNT(*) FROM Customers", connection))
                {
                    int count = (int)await command.ExecuteScalarAsync(); // compliant
                    Console.WriteLine($"Total customers: {count}");
                }
            }
        }

        public async Task Good_Use_EntityFramework_Async()
        {
            // ✅ Compliant: Use EF Core LINQ query with async
            using (var context = new AppDbContext())
            {
                int count = await context.Customers.CountAsync(); // compliant
                Console.WriteLine($"Total customers via EF: {count}");
            }
        }
    }
}
