using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class DateDiffFunctionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded DATEDIFF() function in SQL Server
        public void Bad_HardCodedDateDiff()
        {
            string sql = "SELECT Name, DATEDIFF(day, CreatedDate, GETDATE()) AS DaysSinceCreation FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Name: {reader["Name"]}, DaysSinceCreation: {reader["DaysSinceCreation"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL with DATEDIFF() concatenation
        public void Bad_DynamicDateDiff(string columnName)
        {
            string sql = $"SELECT Name, DATEDIFF(day, {columnName}, GETDATE()) AS DaysDiff FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Name: {reader["Name"]}, DaysDiff: {reader["DaysDiff"]}");
                }
            }
        }

        // ✅ Compliant: Application-side date difference calculation
        public async Task Good_ApplicationDateDiff()
        {
            string sql = "SELECT Name, CreatedDate FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    DateTime createdDate = (DateTime)reader["CreatedDate"];
                    int daysSinceCreation = (DateTime.UtcNow - createdDate).Days;
                    Console.WriteLine($"Name: {reader["Name"]}, DaysSinceCreation: {daysSinceCreation}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ date difference calculation
        public async Task Good_EntityFrameworkDateDiff(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Select(u => new
                                       {
                                           u.Name,
                                           DaysSinceCreation = (int)(DateTime.UtcNow - u.CreatedDate).TotalDays
                                       })
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"Name: {user.Name}, DaysSinceCreation: {user.DaysSinceCreation}");
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
