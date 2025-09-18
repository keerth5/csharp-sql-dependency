using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FromSqlInterpolatedMethodSample
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=TestDb;Trusted_Connection=True;");
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new AppDbContext())
            {
                // ❌ VIOLATION: Raw SQL with string interpolation
                var users1 = context.Users
                                    .FromSqlInterpolated($"SELECT * FROM Users WHERE Id = {1}")
                                    .ToList();

                foreach (var u in users1)
                {
                    Console.WriteLine("User (violation): " + u.UserName);
                }

                // ❌ VIOLATION: Interpolated variable (safer but still raw SQL usage)
                int userId = 2;
                var users2 = context.Users
                                    .FromSqlInterpolated($"SELECT * FROM Users WHERE Id = {userId}")
                                    .ToList();

                foreach (var u in users2)
                {
                    Console.WriteLine("User (violation): " + u.UserName);
                }

                // ✅ COMPLIANT: LINQ query for safe, portable access
                var safeUsers = context.Users.Where(u => u.Id == 3).ToList();
                foreach (var u in safeUsers)
                {
                    Console.WriteLine("User (compliant): " + u.UserName);
                }

                // ✅ COMPLIANT: Async EF Core query
                Task.Run(async () =>
                {
                    var asyncUsers = await context.Users
                                                  .Where(u => u.UserName.StartsWith("A"))
                                                  .ToListAsync();
                    foreach (var u in asyncUsers)
                    {
                        Console.WriteLine("Async User (compliant): " + u.UserName);
                    }
                }).Wait();
            }
        }
    }
}
