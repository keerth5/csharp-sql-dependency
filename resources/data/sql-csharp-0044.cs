using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SampleApp
{
    public class ExecStatementUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded EXEC statement in SQL
        public void Bad_HardCodedExec()
        {
            string sql = "EXEC GetActiveUsers";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}");
                }
            }
        }

        // ❌ Violation: Dynamic EXEC with string concatenation
        public void Bad_DynamicExec(string procedureName)
        {
            string sql = $"EXEC {procedureName}";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}");
                }
            }
        }

        // ✅ Compliant: Parameterized SqlCommand with CommandType.StoredProcedure
        public async Task Good_StoredProcedureCall(string procedureName, SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}");
                }
            }
        }

        // ✅ Compliant: Using EF Core to call stored procedure
        public async Task Good_EFStoredProcedureCall(MyDbContext dbContext, string role)
        {
            var users = await dbContext.Users
                                       .FromSqlRaw("EXEC GetUsersByRole @Role", new SqlParameter("@Role", role))
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
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> FromSqlRaw(string sql, params SqlParameter[] parameters) => this;
    }
}
