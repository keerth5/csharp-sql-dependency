using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class OuterApplyUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using OUTER APPLY in SQL Server query
        public void Bad_OuterApplyQuery()
        {
            string sql = @"
                SELECT e.EmployeeId, e.Name, s.SalesAmount
                FROM Employees e
                OUTER APPLY
                (
                    SELECT TOP 1 SalesAmount
                    FROM Sales s
                    WHERE s.EmployeeId = e.EmployeeId
                    ORDER BY s.Date DESC
                ) AS s;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"EmployeeId: {reader["EmployeeId"]}, Name: {reader["Name"]}, Sales: {reader["SalesAmount"]}");
                }
            }
        }

        // ❌ Violation: Dynamic OUTER APPLY SQL
        public void Bad_DynamicOuterApply(string topCountColumn)
        {
            string sql = $@"
                SELECT e.EmployeeId, e.Name, s.SalesAmount
                FROM Employees e
                OUTER APPLY
                (
                    SELECT TOP {topCountColumn} SalesAmount
                    FROM Sales s
                    WHERE s.EmployeeId = e.EmployeeId
                    ORDER BY s.Date DESC
                ) AS s;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"EmployeeId: {reader["EmployeeId"]}, Name: {reader["Name"]}, Sales: {reader["SalesAmount"]}");
                }
            }
        }

        // ✅ Compliant: Application-side join simulating OUTER APPLY
        public async Task Good_ApplicationOuterJoin(MyDbContext dbContext)
        {
            var employees = await dbContext.Employees.ToListAsync();
            var sales = await dbContext.Sales.ToListAsync();

            var latestSales = employees.Select(e =>
            {
                var latest = sales
                    .Where(s => s.EmployeeId == e.EmployeeId)
                    .OrderByDescending(s => s.Date)
                    .FirstOrDefault();
                return new
                {
                    e.EmployeeId,
                    e.Name,
                    SalesAmount = latest?.SalesAmount ?? 0
                };
            }).ToList();

            foreach (var row in latestSales)
            {
                Console.WriteLine($"EmployeeId: {row.EmployeeId}, Name: {row.Name}, Sales: {row.SalesAmount}");
            }
        }

        // ✅ Compliant: EF Core LINQ join instead of OUTER APPLY
        public async Task Good_EFCoreOuterJoin(MyDbContext dbContext)
        {
            var latestSales = await dbContext.Employees
                .Select(e => new
                {
                    e.EmployeeId,
                    e.Name,
                    SalesAmount = dbContext.Sales
                        .Where(s => s.EmployeeId == e.EmployeeId)
                        .OrderByDescending(s => s.Date)
                        .Select(s => s.SalesAmount)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var row in latestSales)
            {
                Console.WriteLine($"EmployeeId: {row.EmployeeId}, Name: {row.Name}, Sales: {row.SalesAmount}");
            }
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<Employee> Employees { get; set; } = new DbSet<Employee>();
        public DbSet<Sale> Sales { get; set; } = new DbSet<Sale>();
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Sale
    {
        public int EmployeeId { get; set; }
        public decimal SalesAmount { get; set; }
        public DateTime Date { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
        public DbSet<T> Where(Func<T, bool> predicate) => this;
    }
}
