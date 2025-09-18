using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class DeleteStatementUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded DELETE statement in code
        public void Bad_HardCodedDelete()
        {
            string sql = "DELETE FROM Users WHERE Name = 'Alice'";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows deleted (violation): " + rowsAffected);
            }
        }

        // ❌ Violation: Dynamic DELETE with string concatenation
        public void Bad_DynamicDelete(string userName)
        {
            string sql = $"DELETE FROM Users WHERE Name = '{userName}'";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows deleted (violation): " + rowsAffected);
            }
        }

        // ✅ Compliant: Parameterized DELETE query
        public async Task Good_ParameterizedDelete(string userName)
        {
            string sql = "DELETE FROM Users WHERE Name = @Name";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Name", userName);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                Console.WriteLine("Rows deleted (compliant): " + rowsAffected);
            }
        }

        // ✅ Compliant: Using EF Core for deletion
        public async Task Good_EntityFrameworkDelete(MyDbContext dbContext, string userName)
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Name == userName);
            if (user != null)
            {
                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync();
                Console.WriteLine("User deleted via EF (compliant).");
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
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public void Remove(T entity) => base.Remove(entity);

        public T FirstOrDefault(Func<T, bool> predicate) => this.FirstOrDefault(predicate);
    }
}
