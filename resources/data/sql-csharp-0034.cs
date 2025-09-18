using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace DapperExecuteMethodSample
{
    // Entity model
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }

    // EF Core context for compliant usage
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=TestDb;Trusted_Connection=True;");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

            // ❌ VIOLATION: Direct use of Dapper Execute method (synchronous, blocking)
            using (var connection = new SqlConnection(connectionString))
            {
                int rows = connection.Execute("UPDATE Users SET UserName = 'Legacy' WHERE Id = 1");
                Console.WriteLine("Rows affected (violation): " + rows);
            }

            // ❌ VIOLATION: Execute with parameters (still synchronous)
            using (var connection = new SqlConnection(connectionString))
            {
                int userId = 2;
                int rows = connection.Execute(
                    "DELETE FROM Users WHERE Id = @Id", new { Id = userId });
                Console.WriteLine("Rows affected (violation): " + rows);
            }

            // ✅ COMPLIANT: EF Core SaveChanges for persistence (portable and safe)
            using (var context = new AppDbContext())
            {
                var user = context.Users.Find(3);
                if (user != null)
                {
                    user.UserName = "UpdatedViaEF";
                    context.SaveChanges();
                    Console.WriteLine("User updated (compliant).");
                }
            }

            // ✅ COMPLIANT: Async EF Core persistence
            Task.Run(async () =>
            {
                using (var context = new AppDbContext())
                {
                    var newUser = new User { UserName = "AsyncUser" };
                    await context.Users.AddAsync(newUser);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Async user saved (compliant).");
                }
            }).Wait();
        }
    }
}
