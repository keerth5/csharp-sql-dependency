using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class RowCountUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using @@ROWCOUNT in SQL Server
        public void Bad_UseRowCount()
        {
            string sql = "UPDATE Users SET IsActive = 1 WHERE LastLogin < GETDATE() - 30; SELECT @@ROWCOUNT AS AffectedRows;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"AffectedRows: {reader["AffectedRows"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL using @@ROWCOUNT
        public void Bad_DynamicRowCount(string tableName)
        {
            string sql = $"DELETE FROM {tableName} WHERE IsActive = 0; SELECT @@ROWCOUNT AS DeletedRows;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"DeletedRows: {reader["DeletedRows"]}");
                }
            }
        }

        // ✅ Compliant: Application-side row count calculation
        public async Task Good_ApplicationRowCount()
        {
            string sql = "SELECT * FROM Users WHERE LastLogin < GETDATE() - 30";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                int count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                }
                Console.WriteLine($"AffectedRows: {count}");
            }
        }

        // ✅ Compliant: EF Core row count
        public async Task Good_EntityFrameworkRowCount(MyDbContext dbContext)
        {
            int affectedRows = await dbContext.Users
                                              .Where(u => u.LastLogin < DateTime.UtcNow.AddDays(-30))
                                              .CountAsync();

            Console.WriteLine($"AffectedRows: {affectedRows}");
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
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<int> CountAsync() => Task.FromResult(this.Count);
        public DbSet<T> Where(Func<T, bool> predicate) => this;
    }
}
