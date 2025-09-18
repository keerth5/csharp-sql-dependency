using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class InsertStatementUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded INSERT statement in code
        public void Bad_HardCodedInsert()
        {
            string sql = "INSERT INTO Users (Name, Role, IsActive) VALUES ('Alice', 'Admin', 1)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows inserted (violation): " + rowsAffected);
            }
        }

        // ❌ Violation: Dynamic INSERT using concatenation
        public void Bad_DynamicInsert(string userName, string role)
        {
            string sql = $"INSERT INTO Users (Name, Role, IsActive) VALUES ('{userName}', '{role}', 1)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("Rows inserted (violation): " + rowsAffected);
            }
        }

        // ✅ Compliant: Using parameterized query
        public async Task Good_ParameterizedInsert(string userName, string role)
        {
            string sql = "INSERT INTO Users (Name, Role, IsActive) VALUES (@Name, @Role, @IsActive)";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Name", userName);
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@IsActive", true);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                Console.WriteLine("Rows inserted (compliant): " + rowsAffected);
            }
        }

        // ✅ Compliant: Using EF Core AddAsync for insertion
        public async Task Good_EntityFrameworkInsert(MyDbContext dbContext, string userName, string role)
        {
            var user = new User { Name = userName, Role = role, IsActive = true };
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("User inserted via EF (compliant).");
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<User> Users { get; set; }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    public class User
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T>
    {
        public Task AddAsync(T entity) => Task.CompletedTask;
    }
}
