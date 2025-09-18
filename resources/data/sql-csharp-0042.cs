using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class WhereClauseUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded WHERE clause in SQL statement
        public void Bad_HardCodedWhere()
        {
            string sql = "SELECT Name, Role FROM Users WHERE IsActive = 1";

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

        // ❌ Violation: Dynamic WHERE clause using string concatenation
        public void Bad_DynamicWhere(string userName)
        {
            string sql = "SELECT Name, Role FROM Users WHERE Name = '" + userName + "'";

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

        // ✅ Compliant: Parameterized WHERE clause
        public async Task Good_ParameterizedWhere(string userName)
        {
            string sql = "SELECT Name, Role FROM Users WHERE Name = @Name";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Name", userName);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["Role"]}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ abstraction
        public async Task Good_EntityFrameworkWhere(MyDbContext dbContext, string role)
        {
            var users = await dbContext.Users
                                       .Where(u => u.Role == role && u.IsActive)
                                       .Select(u => new { u.Name, u.Role })
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
