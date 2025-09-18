using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class OffsetFetchUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Hard-coded OFFSET/FETCH SQL Server pagination
        public void Bad_HardCodedOffsetFetch(int offset, int fetch)
        {
            string sql = $"SELECT Name, CreatedDate FROM Users ORDER BY CreatedDate OFFSET {offset} ROWS FETCH NEXT {fetch} ROWS ONLY";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Name: {reader["Name"]}, CreatedDate: {reader["CreatedDate"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL using OFFSET/FETCH
        public void Bad_DynamicOffsetFetch(int pageIndex, int pageSize)
        {
            int offset = pageIndex * pageSize;
            string sql = $"SELECT Name, CreatedDate FROM Users ORDER BY CreatedDate OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"Name: {reader["Name"]}, CreatedDate: {reader["CreatedDate"]}");
                }
            }
        }

        // ✅ Compliant: Application-side pagination
        public async Task Good_ApplicationPagination(int pageIndex, int pageSize)
        {
            string sql = "SELECT Name, CreatedDate FROM Users ORDER BY CreatedDate";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                var users = new System.Collections.Generic.List<(string Name, DateTime CreatedDate)>();
                while (await reader.ReadAsync())
                {
                    users.Add((reader["Name"].ToString(), (DateTime)reader["CreatedDate"]));
                }

                var pagedUsers = users.Skip(pageIndex * pageSize).Take(pageSize);
                foreach (var user in pagedUsers)
                {
                    Console.WriteLine($"Name: {user.Name}, CreatedDate: {user.CreatedDate}");
                }
            }
        }

        // ✅ Compliant: EF Core LINQ pagination
        public async Task Good_EntityFrameworkPagination(MyDbContext dbContext, int pageIndex, int pageSize)
        {
            var users = await dbContext.Users
                                       .OrderBy(u => u.CreatedDate)
                                       .Skip(pageIndex * pageSize)
                                       .Take(pageSize)
                                       .ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine($"Name: {user.Name}, CreatedDate: {user.CreatedDate}");
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
        public DateTime CreatedDate { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Skip(int count) => this;
        public DbSet<T> Take(int count) => this;
        public DbSet<T> OrderBy<U>(Func<T, U> keySelector) => this;
    }
}
