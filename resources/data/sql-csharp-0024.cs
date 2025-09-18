using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExecuteQueryMethodExample
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

    // ❌ Example of a custom blocking ExecuteQuery helper
    public class DbHelper
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public List<string> ExecuteQuery(string sql) // violation target
        {
            var results = new List<string>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(reader[0].ToString());
                    }
                }
            }
            return results;
        }

        public async Task<List<string>> ExecuteQueryAsync(string sql) // compliant
        {
            var results = new List<string>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(reader[0].ToString());
                    }
                }
            }
            return results;
        }
    }

    public class ExecuteQueryTest
    {
        public void Bad_Use_Custom_ExecuteQuery()
        {
            // ❌ Violation: Synchronous ExecuteQuery blocks threads
            var db = new DbHelper();
            var orders = db.ExecuteQuery("SELECT Product FROM Orders"); // violation
            foreach (var o in orders)
            {
                Console.WriteLine(o);
            }
        }

        public async Task Good_Use_Custom_ExecuteQueryAsync()
        {
            // ✅ Compliant: Use async ExecuteQueryAsync()
            var db = new DbHelper();
            var orders = await db.ExecuteQueryAsync("SELECT Product FROM Orders"); // compliant
            foreach (var o in orders)
            {
                Console.WriteLine(o);
            }
        }

        public async Task Good_Use_EntityFramework_Async()
        {
            // ✅ Compliant: Use EF Core async queries
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
