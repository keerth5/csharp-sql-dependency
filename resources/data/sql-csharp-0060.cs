using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class TableVariableUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using table variable in SQL Server
        public void Bad_UseTableVariable()
        {
            string sql = @"
                DECLARE @TempUsers TABLE (Id INT, Name NVARCHAR(100));
                INSERT INTO @TempUsers (Id, Name) SELECT Id, Name FROM Users;
                SELECT * FROM @TempUsers;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, Name: {reader["Name"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL with table variable
        public void Bad_DynamicTableVariable(string tableName)
        {
            string sql = $@"
                DECLARE @Temp TABLE (Id INT, Name NVARCHAR(100));
                INSERT INTO @Temp (Id, Name) SELECT Id, Name FROM {tableName};
                SELECT * FROM @Temp;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, Name: {reader["Name"]}");
                }
            }
        }

        // ✅ Compliant: Use application-side in-memory collections
        public async Task Good_ApplicationInMemory()
        {
            string sql = "SELECT Id, Name FROM Users";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                var tempUsers = new System.Collections.Generic.List<(int Id, string Name)>();
                while (await reader.ReadAsync())
                {
                    tempUsers.Add(((int)reader["Id"], reader["Name"].ToString()));
                }

                foreach (var user in tempUsers)
                {
                    Console.WriteLine($"Id: {user.Id}, Name: {user.Name}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ queries without table variables
        public async Task Good_EntityFrameworkInMemory(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Select(u => new { u.Id, u.Name })
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"Id: {user.Id}, Name: {user.Name}");
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
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Select<U>(Func<T, U> selector) => this;
    }
}
