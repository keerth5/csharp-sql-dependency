using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SampleApp
{
    public class SelectStatementUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded SELECT statement in code
        public void Bad_HardCodedSelect()
        {
            string sql = "SELECT * FROM Users WHERE IsActive = 1"; // Direct SELECT usage

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

        // ❌ Violation: Inline SELECT with string concatenation
        public void Bad_InlineSelectWithUserInput(string userInput)
        {
            string sql = "SELECT * FROM Users WHERE Name = '" + userInput + "'"; // Dangerous SELECT

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

        // ✅ Compliant: Using a stored procedure instead of SELECT
        public async Task Good_UseStoredProcedure(string role)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("GetActiveUsersByRole", connection)) // stored procedure
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Role", role);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["Role"]}");
                }
            }
        }

        // ✅ Compliant: Using Entity Framework LINQ for portability
        public async Task Good_UseEntityFramework(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Where(u => u.IsActive && u.Role == "Admin")
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"User: {user.Name}, Role: {user.Role}");
            }
        }
    }

    // Example EF DbContext for demonstration
    public class MyDbContext
    {
        public IQueryable<User> Users => new List<User>
        {
            new User { Name = "Alice", Role = "Admin", IsActive = true },
            new User { Name = "Bob", Role = "User", IsActive = false }
        }.AsQueryable();
    }

    public class User
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}
