using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class RollbackTransactionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Explicit ROLLBACK in SQL Server transaction
        public void Bad_ExplicitRollback()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var command = new SqlCommand("UPDATE Users SET IsActive = 1 WHERE LastLogin < GETDATE() - 30", connection, transaction);
                        command.ExecuteNonQuery();

                        // Commit normally
                        transaction.Commit();
                    }
                    catch
                    {
                        // Explicit ROLLBACK
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ❌ Violation: Dynamic SQL with ROLLBACK
        public void Bad_DynamicRollback(string tableName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var sql = $"DELETE FROM {tableName} WHERE IsActive = 0";
                        var command = new SqlCommand(sql, connection, transaction);
                        command.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        // Explicit ROLLBACK
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ✅ Compliant: EF Core handles rollback via BeginTransactionAsync
        public async Task Good_EntityFrameworkRollback(MyDbContext dbContext)
        {
            using (var transaction = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var users = await dbContext.Users
                                               .Where(u => u.LastLogin < DateTime.UtcNow.AddDays(-30))
                                               .ToListAsync();

                    foreach (var user in users)
                    {
                        user.IsActive = true;
                    }

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    // Vendor-independent rollback
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        // ✅ Compliant: Unit-of-work pattern without explicit ROLLBACK
        public async Task Good_UnitOfWorkRollback(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Where(u => u.LastLogin < DateTime.UtcNow.AddDays(-30))
                                       .ToListAsync();

            foreach (var user in users)
            {
                user.IsActive = true;
            }

            await dbContext.SaveChangesAsync();
            // EF Core handles rollback internally in case of exceptions
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<User> Users { get; set; } = new DbSet<User>();
        public DatabaseFacade Database { get; set; } = new DatabaseFacade();
    }

    public class User
    {
        public string Name { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; }
    }

    // Mock DbSet and DatabaseFacade for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Where(Func<T, bool> predicate) => this;
    }

    public class DatabaseFacade
    {
        public Task<Transaction> BeginTransactionAsync() => Task.FromResult(new Transaction());
    }

    public class Transaction : IDisposable
    {
        public Task CommitAsync() => Task.CompletedTask;
        public Task RollbackAsync() => Task.CompletedTask;
        public void Dispose() { }
    }
}
