using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class IsNullFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded ISNULL() function in SQL Server
        public void Bad_HardCodedIsNull()
        {
            string sql = "SELECT Name, ISNULL(Role, 'Guest') AS Role FROM Users WHERE IsActive = 1";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["Role"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL using ISNULL() concatenation
        public void Bad_DynamicIsNull(string columnName)
        {
            string sql = $"SELECT Name, ISNULL({columnName}, 'Unknown') AS Value FROM Users WHERE IsActive = 1";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Value: {reader["Value"]}");
                }
            }
        }

        // ✅ Compliant: Use COALESCE() for standard SQL null handling
        public async Task Good_CoalesceUsage(string columnName)
        {
            string sql = $"SELECT Name, COALESCE({columnName}, 'Unknown') AS Value FROM Users WHERE IsActive = @IsActive";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@IsActive", true);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Value: {reader["Value"]}");
                }
            }
        }

        // ✅ Compliant: EF Core null coalescing in LINQ
        public async Task Good_EntityFrameworkNullHandling(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Where(u => u.IsActive)
                                       .Select(u => new
                                       {
                                           u.Name,
                                           Role = u.Role ?? "Guest"
                                       })
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"User: {user.Name}, Role: {user.Role}");
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
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Where(Func<T, bool> predicate) => new DbSet<T>(this.Where(predicate).ToList());
        public DbSet() { }
        public DbSet(System.Collections.Generic.List<T> items) : base(items) { }
        public DbSet<T> Select<U>(Func<T, U> selector) => this;
    }
}
