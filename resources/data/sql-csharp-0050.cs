using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SampleApp
{
    public class GetUtcDateFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded GETUTCDATE() function in SQL Server
        public void Bad_HardCodedGetUtcDate()
        {
            string sql = "INSERT INTO Users (Name, Role, CreatedUtc) VALUES ('Alice', 'Admin', GETUTCDATE())";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows inserted (violation): " + rowsAffected);
            }
        }

        // ❌ Violation: Dynamic SQL with GETUTCDATE() concatenation
        public void Bad_DynamicGetUtcDate(string name, string role)
        {
            string sql = $"INSERT INTO Users (Name, Role, CreatedUtc) VALUES ('{name}', '{role}', GETUTCDATE())";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows inserted (violation): " + rowsAffected);
            }
        }

        // ✅ Compliant: Use application-side DateTime.UtcNow
        public async Task Good_ApplicationUtcDate(string name, string role)
        {
            string sql = "INSERT INTO Users (Name, Role, CreatedUtc) VALUES (@Name, @Role, @CreatedUtc)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@CreatedUtc", DateTime.UtcNow);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                Console.WriteLine("Rows inserted (compliant): " + rowsAffected);
            }
        }

        // ✅ Compliant: EF Core sets DateTime.UtcNow in code
        public async Task Good_EntityFrameworkInsert(MyDbContext dbContext, string name, string role)
        {
            var user = new User
            {
                Name = name,
                Role = role,
                CreatedUtc = DateTime.UtcNow
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("User inserted via EF (compliant).");
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<User> Users { get; set; } = new DbSet<User>();

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    public class User
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public void Add(T item) => base.Add(item);
    }
}
