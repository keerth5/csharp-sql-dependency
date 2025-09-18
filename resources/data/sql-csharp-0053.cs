using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class CastFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded CAST() function in SQL Server
        public void Bad_HardCodedCast()
        {
            string sql = "SELECT Name, CAST(CreatedDate AS VARCHAR) AS CreatedDateStr FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, CreatedDateStr: {reader["CreatedDateStr"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL with CAST() concatenation
        public void Bad_DynamicCast(string columnName)
        {
            string sql = $"SELECT Name, CAST({columnName} AS VARCHAR) AS ConvertedValue FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, ConvertedValue: {reader["ConvertedValue"]}");
                }
            }
        }

        // ✅ Compliant: Use application-side conversion
        public async Task Good_ApplicationConversion()
        {
            string sql = "SELECT Name, CreatedDate FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string createdDateStr = ((DateTime)reader["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"User: {reader["Name"]}, CreatedDateStr: {createdDateStr}");
                }
            }
        }

        // ✅ Compliant: EF Core handles conversion in code
        public async Task Good_EntityFrameworkConversion(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Select(u => new
                                       {
                                           u.Name,
                                           CreatedDateStr = u.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                                       })
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"User: {user.Name}, CreatedDateStr: {user.CreatedDateStr}");
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
