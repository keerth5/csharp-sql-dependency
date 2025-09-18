using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExecuteSqlInterpolatedMethodSample
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
                // ❌ VIOLATION: Interpolated SQL execution
                context.Database.ExecuteSqlInterpolated($"DELETE FROM Users WHERE Id = {1}");

                // ❌ VIOLATION: Variable interpolation still raw SQL execution
                int userId = 2;
                context.Database.ExecuteSqlInterpolated($"DELETE FROM Users WHERE Id = {userId}");

                // ✅ COMPLIANT: Using EF Core LINQ for safe and portable deletion
                var user = context.Users.FirstOrDefault(u => u.Id == 3);
                if (user != null)
                {
                    context.Users.Remove(user);
                    context.SaveChanges();
                }

                // ✅ COMPLIANT: Async-safe EF Core data manipulation
                Task.Run(async () =>
                {
                    var newUser = new User { UserName = "Alice" };
                    await context.Users.AddAsync(newUser);
                    await context.SaveChangesAsync();
                }).Wait();
            }
        }
    }
}
