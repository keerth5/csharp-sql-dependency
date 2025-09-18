using System;
using System.Data;
using System.Data.SqlClient; // Direct SQL command usage
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SqlCommandUsageExample
{
    // Entity Framework DbContext (Compliant)
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
    }

    public class Customer 
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SqlCommandUsageTest
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public void Bad_Use_SqlCommand_Sync()
        {
            // ❌ Violation: Using SqlCommand directly (synchronous)
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT * FROM Customers", connection))
                {
                    using (var reader = command.ExecuteReader()) // violation
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Name"]);
                        }
                    }
                }
            }
        }

        public async Task Bad_Use_SqlCommand_Async()
        {
            // ❌ Violation: Even async still uses SqlCommand directly
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT * FROM Customers", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync()) // violation
                    {
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine(reader["Name"]);
                        }
                    }
                }
            }
        }

        public async Task Good_Use_EntityFramework_Async()
        {
            // ✅ Compliant: Using EF Core (portable, async-ready)
            using (var context = new AppDbContext())
            {
                var customers = await context.Customers.ToListAsync();
                foreach (var c in customers)
                {
                    Console.WriteLine(c.Name);
                }
            }
        }

        public void Good_Use_StoredProcedure()
        {
            // ✅ Compliant alternative: Using DbContext for stored procedures
            using (var context = new AppDbContext())
            {
                var result = context.Customers
                    .FromSqlRaw("EXEC GetAllCustomers")
                    .ToList();
                
                foreach (var c in result)
                {
                    Console.WriteLine(c.Name);
                }
            }
        }
    }
}
