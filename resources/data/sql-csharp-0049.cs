using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class GetDateFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded GETDATE() function in SQL Server
        public void Bad_HardCodedGetDate()
        {
            string sql = "INSERT INTO Users (Name, Role, CreatedDate) VALUES ('Alice', 'Admin', GETDATE())";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows inserted (violation): " + rowsAffected);
            }
        }

        // ❌ Violation: Dynamic SQL with GETDATE() concatenation
        public void Bad_DynamicGetDate(string name, string role)
        {
            string sql = $"INSERT INTO Users (Name, Role, CreatedDate) VALUES ('{name}', '{role}', GETDATE())";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows inserted (violation): " + rowsAffected);
            }
        }

        // ✅ Compliant: Use application-side DateTime
        public async Task Good_ApplicationDateTime(string name, string role)
        {
            string sql = "INSERT INTO Users (Name, Role, CreatedDate) VALUES (@Name, @Role, @CreatedDate)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                Console.WriteLine("Rows inserted (compliant): " + rowsAffected);
            }
        }

        // ✅ Compliant: EF Core sets DateTime in code
        public async Task Good_EntityFrameworkInsert(MyDbContext dbContext, string name, string role)
        {
            var user = new User
            {
                Name = name,
                Role = role,
                CreatedDate = DateTime.UtcNow
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
        public DateTime CreatedDate { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public void Add(T item) => base.Add(item);
    }
}
