using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class PivotUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using PIVOT operator in SQL Server
        public void Bad_PivotQuery()
        {
            string sql = @"
                SELECT *
                FROM 
                (
                    SELECT EmployeeId, Month, SalesAmount
                    FROM EmployeeSales
                ) AS SourceTable
                PIVOT
                (
                    SUM(SalesAmount)
                    FOR Month IN ([Jan], [Feb], [Mar])
                ) AS PivotTable;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"EmployeeId: {reader["EmployeeId"]}");
                }
            }
        }

        // ❌ Violation: Dynamic SQL with PIVOT
        public void Bad_DynamicPivot(string monthColumn)
        {
            string sql = $@"
                SELECT *
                FROM 
                (
                    SELECT EmployeeId, {monthColumn}, SalesAmount
                    FROM EmployeeSales
                ) AS SourceTable
                PIVOT
                (
                    SUM(SalesAmount)
                    FOR {monthColumn} IN ([Jan], [Feb], [Mar])
                ) AS PivotTable;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"EmployeeId: {reader["EmployeeId"]}");
                }
            }
        }

        // ✅ Compliant: Application-side pivot using in-memory collections
        public async Task Good_ApplicationPivot()
        {
            var sales = new System.Collections.Generic.List<(int EmployeeId, string Month, decimal SalesAmount)>
            {
                (1, "Jan", 100),
                (1, "Feb", 150),
                (2, "Jan", 200),
                (2, "Mar", 300)
            };

            var pivoted = sales
                .GroupBy(s => s.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Jan = g.Where(x => x.Month == "Jan").Sum(x => x.SalesAmount),
                    Feb = g.Where(x => x.Month == "Feb").Sum(x => x.SalesAmount),
                    Mar = g.Where(x => x.Month == "Mar").Sum(x => x.SalesAmount)
                }).ToList();

            foreach (var row in pivoted)
            {
                Console.WriteLine($"EmployeeId: {row.EmployeeId}, Jan: {row.Jan}, Feb: {row.Feb}, Mar: {row.Mar}");
            }
        }

        // ✅ Compliant: EF Core LINQ without PIVOT
        public async Task Good_EFCorePivot(MyDbContext dbContext)
        {
            var sales = await dbContext.EmployeeSales
                                       .ToListAsync();

            var pivoted = sales
                .GroupBy(s => s.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Jan = g.Where(x => x.Month == "Jan").Sum(x => x.SalesAmount),
                    Feb = g.Where(x => x.Month == "Feb").Sum(x => x.SalesAmount),
                    Mar = g.Where(x => x.Month == "Mar").Sum(x => x.SalesAmount)
                }).ToList();

            foreach (var row in pivoted)
            {
                Console.WriteLine($"EmployeeId: {row.EmployeeId}, Jan: {row.Jan}, Feb: {row.Feb}, Mar: {row.Mar}");
            }
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<EmployeeSale> EmployeeSales { get; set; } = new DbSet<EmployeeSale>();
    }

    public class EmployeeSale
    {
        public int EmployeeId { get; set; }
        public string Month { get; set; } = string.Empty;
        public decimal SalesAmount { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
    }
}
