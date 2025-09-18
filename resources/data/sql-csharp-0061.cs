using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SampleApp
{
    public class BeginTransactionUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using SQL Server-specific BeginTransaction
        public void Bad_ExplicitSqlTransaction()
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

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ❌ Violation: Dynamic transaction with SQL Server
        public void Bad_DynamicTransaction(string tableName)
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
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ✅ Compliant: Application-managed transaction using EF Core
        public async Task Good_EntityFrameworkTransaction(MyDbContext dbContext)
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
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        // ✅ Compliant: Unit-of-work pattern without explicit SQL transaction
        public async Task Good_UnitOfWorkTransaction(MyDbContext dbContext)
        {
            var users = await dbContext.Users
                                       .Where(u => u.LastLogin < DateTime.UtcNow.AddDays(-30))
                                       .ToListAsync();

            foreach (var user in users)
            {
                user.IsActive = true;
            }

            await dbContext.SaveChangesAsync();
            // Transaction management handled internally by EF Core
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
