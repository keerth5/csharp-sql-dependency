using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class UpdateStatementUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded UPDATE statement in code
        public void Bad_HardCodedUpdate()
        {
            string sql = "UPDATE Users SET Role = 'Admin' WHERE Name = 'Alice'";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows updated (violation): " + rowsAffected);
            }
        }

        // ❌ Violation: Dynamic UPDATE with string concatenation
        public void Bad_DynamicUpdate(string userName, string role)
        {
            string sql = $"UPDATE Users SET Role = '{role}' WHERE Name = '{userName}'";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows updated (violation): " + rowsAffected);
            }
        }

        // ✅ Compliant: Parameterized UPDATE query
        public async Task Good_ParameterizedUpdate(string userName, string role)
        {
            string sql = "UPDATE Users SET Role = @Role WHERE Name = @Name";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@Name", userName);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                Console.WriteLine("Rows updated (compliant): " + rowsAffected);
            }
        }

        // ✅ Compliant: Using EF Core for update
        public async Task Good_EntityFrameworkUpdate(MyDbContext dbContext, string userName, string role)
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Name == userName);
            if (user != null)
            {
                user.Role = role;
                await dbContext.SaveChangesAsync();
                Console.WriteLine("User updated via EF (compliant).");
            }
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
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public T FirstOrDefault(Func<T, bool> predicate) => this.FirstOrDefault(predicate);
    }
}
