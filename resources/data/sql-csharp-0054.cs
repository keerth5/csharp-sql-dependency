using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class LeftFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded LEFT() function in SQL Server
        public void Bad_HardCodedLeft()
        {
            string sql = "SELECT LEFT(Name, 5) AS ShortName FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"ShortName: {reader["ShortName"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL with LEFT() concatenation
        public void Bad_DynamicLeft(string columnName, int length)
        {
            string sql = $"SELECT LEFT({columnName}, {length}) AS SubStringValue FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"SubStringValue: {reader["SubStringValue"]}");
                }
            }
        }

        // ✅ Compliant: Application-side string manipulation
        public async Task Good_ApplicationSubstring()
        {
            string sql = "SELECT Name FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string shortName = reader["Name"].ToString().Substring(0, Math.Min(5, reader["Name"].ToString().Length));
                    Console.WriteLine($"ShortName: {shortName}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ string manipulation
        public async Task Good_EntityFrameworkSubstring(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Select(u => new
                                       {
                                           ShortName = u.Name.Length > 5 ? u.Name.Substring(0, 5) : u.Name
                                       })
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"ShortName: {user.ShortName}");
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
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Select<U>(Func<T, U> selector) => this;
    }
}
