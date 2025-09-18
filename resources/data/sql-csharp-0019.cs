using System;
using System.Data.SqlClient; // direct DB usage
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExecuteReaderMethodExample
{
    // EF Core DbContext (Compliant ORM abstraction)
    public class AppDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string Product { get; set; }
    }

    public class ExecuteReaderTest
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public void Bad_Use_ExecuteReader_Sync()
        {
            // ❌ Violation: ExecuteReader() is synchronous and blocks threads
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT * FROM Orders", connection))
                {
                    using (var reader = command.ExecuteReader()) // violation
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Product"]);
                        }
                    }
                }
            }
        }

        public async Task Good_Use_ExecuteReader_Async()
        {
            // ✅ Compliant: Use ExecuteReaderAsync() for non-blocking IO
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT * FROM Orders", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync()) // compliant
                    {
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine(reader["Product"]);
                        }
                    }
                }
            }
        }

        public async Task Good_Use_EntityFramework_Async()
        {
            // ✅ Compliant: Use EF Core which handles async under the hood
            using (var context = new AppDbContext())
            {
                var orders = await context.Orders.ToListAsync();
                foreach (var o in orders)
                {
                    Console.WriteLine(o.Product);
                }
            }
        }
    }
}
