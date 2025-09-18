using System;
using System.Data;
using System.Data.SqlClient; // direct DB usage
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CommandTextPropertyExample
{
    // EF Core DbContext (Compliant ORM abstraction)
    public class AppDbContext : DbContext
    {
        public DbSet<UserAccount> Users { get; set; }
    }

    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }

    public class CommandTextPropertyTest
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public void Bad_Use_CommandText_DirectAssignment(string userInput)
        {
            // ❌ Violation: Direct string concatenation in CommandText (SQL Injection risk)
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT * FROM Users WHERE Username = '" + userInput + "'"; // violation
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Username"]);
                        }
                    }
                }
            }
        }

        public void Good_Use_CommandText_WithParameters(string userInput)
        {
            // ✅ Compliant: Use parameterized query
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT * FROM Users WHERE Username = @username"; // compliant
                    command.Parameters.AddWithValue("@username", userInput);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Username"]);
                        }
                    }
                }
            }
        }

        public void Good_Use_StoredProcedure(string userInput)
        {
            // ✅ Compliant: Use stored procedure
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("GetUserByUsername", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@username", userInput);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Username"]);
                        }
                    }
                }
            }
        }

        public async Task Good_Use_EntityFramework_Async(string userInput)
        {
            // ✅ Compliant: Use EF Core (ORM)
            using (var context = new AppDbContext())
            {
                var users = await context.Users
                    .FromSqlRaw("EXEC GetUserByUsername @p0", userInput)
                    .ToListAsync();

                foreach (var u in users)
                {
                    Console.WriteLine(u.Username);
                }
            }
        }
    }
}
