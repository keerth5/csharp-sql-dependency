using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class JoinClauseUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded JOIN in SQL statement
        public void Bad_HardCodedJoin()
        {
            string sql = @"
                SELECT u.Name, r.RoleName
                FROM Users u
                INNER JOIN Roles r ON u.Role = r.Id
                WHERE u.IsActive = 1";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["RoleName"]}");
                }
            }
        }

        // ❌ Violation: Dynamic JOIN with string concatenation
        public void Bad_DynamicJoin(string roleTable)
        {
            string sql = $@"
                SELECT u.Name, r.RoleName
                FROM Users u
                INNER JOIN {roleTable} r ON u.Role = r.Id
                WHERE u.IsActive = 1";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["RoleName"]}");
                }
            }
        }

        // ✅ Compliant: Using EF Core LINQ with navigation properties
        public async Task Good_EntityFrameworkJoin(MyDbContext dbContext)
        {
            var usersWithRoles = await dbContext.Users
                                                .Where(u => u.IsActive)
                                                .Select(u => new { u.Name, RoleName = u.Role.RoleName })
                                                .ToListAsync();

            foreach (var user in usersWithRoles)
            {
                Console.WriteLine($"User: {user.Name}, Role: {user.RoleName}");
            }
        }

        // ✅ Compliant: Using stored procedure to abstract JOIN
        public async Task Good_StoredProcedureJoin()
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("GetActiveUsersWithRoles", connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["RoleName"]}");
                }
            }
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<User> Users { get; set; } = new DbSet<User>();
        public DbSet<Role> Roles { get; set; } = new DbSet<Role>();
    }

    public class User
    {
        public string Name { get; set; }
        public Role Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Where(Func<T, bool> predicate) => new DbSet<T>(this.Where(predicate).ToList());
        public DbSet() { }
        public DbSet(System.Collections.Generic.List<T> items) : base(items) { }
        public DbSet<T> Select<U>(Func<T, U> selector) => this;
    }
}
