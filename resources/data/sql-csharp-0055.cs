using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class DateAddFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded DATEADD() function in SQL Server
        public void Bad_HardCodedDateAdd()
        {
            string sql = "SELECT Name, DATEADD(day, 7, CreatedDate) AS ExpiryDate FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Name: {reader["Name"]}, ExpiryDate: {reader["ExpiryDate"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL with DATEADD() concatenation
        public void Bad_DynamicDateAdd(int days)
        {
            string sql = $"SELECT Name, DATEADD(day, {days}, CreatedDate) AS ExpiryDate FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Name: {reader["Name"]}, ExpiryDate: {reader["ExpiryDate"]}");
                }
            }
        }

        // ✅ Compliant: Application-side date manipulation
        public async Task Good_ApplicationDateAdd(int days)
        {
            string sql = "SELECT Name, CreatedDate FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    DateTime expiryDate = ((DateTime)reader["CreatedDate"]).AddDays(days);
                    Console.WriteLine($"Name: {reader["Name"]}, ExpiryDate: {expiryDate}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ date manipulation
        public async Task Good_EntityFrameworkDateAdd(MyDbContext dbContext, int days)
        {
            var users = await dbContext.Users
                                       .Select(u => new
                                       {
                                           u.Name,
                                           ExpiryDate = u.CreatedDate.AddDays(days)
                                       })
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"Name: {user.Name}, ExpiryDate: {user.ExpiryDate}");
            }
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<User> Users { get; set; } = new DbSet<User>();
    }

    public class User
    {
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Select<U>(Func<T, U> selector) => this;
    }
}
