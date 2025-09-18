using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class TopClauseUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded TOP clause in SQL Server
        public void Bad_HardCodedTop()
        {
            string sql = "SELECT TOP 10 Name, Role FROM Users WHERE IsActive = 1";

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

        // ❌ Violation: Dynamic TOP clause with string concatenation
        public void Bad_DynamicTop(int rowCount)
        {
            string sql = $"SELECT TOP {rowCount} Name, Role FROM Users WHERE IsActive = 1";

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

        // ✅ Compliant: Parameterized query with standard SQL LIMIT emulation (for SQL Server 2012+ OFFSET-FETCH)
        public async Task Good_StandardSqlLimit(int rowCount)
        {
            string sql = @"
                SELECT Name, Role 
                FROM Users 
                WHERE IsActive = @IsActive
                ORDER BY Name
                OFFSET 0 ROWS FETCH NEXT @RowCount ROWS ONLY";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@IsActive", true);
                command.Parameters.AddWithValue("@RowCount", rowCount);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["Role"]}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ abstraction with Take()
        public async Task Good_EntityFrameworkTake(MyDbContext dbContext, int rowCount)
        {
            var users = await dbContext.Users
                                       .Where(u => u.IsActive)
                                       .OrderBy(u => u.Name)
                                       .Take(rowCount)
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
        public DbSet<T> OrderBy<TKey>(Func<T, TKey> keySelector) => this;
        public DbSet<T> Take(int count) => this;
        public DbSet<T> Select<U>(Func<T, U> selector) => this;
    }
}
