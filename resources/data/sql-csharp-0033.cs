using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace DapperQueryMethodSample
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

            // ❌ VIOLATION: Direct use of Dapper Query method
            using (var connection = new SqlConnection(connectionString))
            {
                var users = connection.Query<User>("SELECT * FROM Users").ToList();
                foreach (var user in users)
                {
                    Console.WriteLine("User (violation): " + user.UserName);
                }
            }

            // ❌ VIOLATION: Query with parameters (still flagged for direct Dapper usage)
            using (var connection = new SqlConnection(connectionString))
            {
                int userId = 1;
                var user = connection.Query<User>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = userId }).FirstOrDefault();
                Console.WriteLine("User (violation): " + user?.UserName);
            }

            // ✅ COMPLIANT: Use EF Core LINQ abstraction for safe querying
            using (var context = new AppDbContext())
            {
                var safeUsers = context.Users.Where(u => u.Id > 0).ToList();
                foreach (var user in safeUsers)
                {
                    Console.WriteLine("User (compliant): " + user.UserName);
                }
            }

            // ✅ COMPLIANT: Async EF Core query for better scalability
            Task.Run(async () =>
            {
                using (var context = new AppDbContext())
                {
                    var asyncUsers = await context.Users
                                                  .Where(u => u.UserName.StartsWith("A"))
                                                  .ToListAsync();
                    foreach (var user in asyncUsers)
                    {
                        Console.WriteLine("Async User (compliant): " + user.UserName);
                    }
                }
            }).Wait();
        }
    }
}
