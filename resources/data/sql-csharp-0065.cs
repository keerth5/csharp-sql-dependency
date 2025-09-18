using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SampleApp
{
    public class UnpivotUsageExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using UNPIVOT operator in SQL Server
        public void Bad_UnpivotQuery()
        {
            string sql = @"
                SELECT EmployeeId, Month, SalesAmount
                FROM 
                (
                    SELECT EmployeeId, Jan, Feb, Mar
                    FROM EmployeeSalesPivot
                ) AS Pivoted
                UNPIVOT
                (
                    SalesAmount FOR Month IN (Jan, Feb, Mar)
                ) AS Unpivoted;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"EmployeeId: {reader["EmployeeId"]}, Month: {reader["Month"]}, Sales: {reader["SalesAmount"]}");
                }
            }
        }

        // ❌ Violation: Dynamic UNPIVOT SQL
        public void Bad_DynamicUnpivot(string[] months)
        {
            string inColumns = string.Join(",", months);
            string sql = $@"
                SELECT EmployeeId, Month, SalesAmount
                FROM 
                (
                    SELECT EmployeeId, {inColumns}
                    FROM EmployeeSalesPivot
                ) AS Pivoted
                UNPIVOT
                (
                    SalesAmount FOR Month IN ({inColumns})
                ) AS Unpivoted;
            ";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"EmployeeId: {reader["EmployeeId"]}, Month: {reader["Month"]}, Sales: {reader["SalesAmount"]}");
                }
            }
        }

        // ✅ Compliant: Application-side unpivot using in-memory collections
        public async Task Good_ApplicationUnpivot()
        {
            var pivotedSales = new System.Collections.Generic.List<(int EmployeeId, decimal Jan, decimal Feb, decimal Mar)>
            {
                (1, 100, 150, 200),
                (2, 200, 250, 300)
            };

            var unpivoted = pivotedSales
                .SelectMany(p => new[]
                {
                    (p.EmployeeId, "Jan", p.Jan),
                    (p.EmployeeId, "Feb", p.Feb),
                    (p.EmployeeId, "Mar", p.Mar)
                })
                .ToList();

            foreach (var row in unpivoted)
            {
                Console.WriteLine($"EmployeeId: {row.EmployeeId}, Month: {row.Item2}, Sales: {row.Item3}");
            }
        }

        // ✅ Compliant: EF Core LINQ without UNPIVOT
        public async Task Good_EFCoreUnpivot(MyDbContext dbContext)
        {
            var pivotedSales = await dbContext.EmployeeSalesPivot.ToListAsync();

            var unpivoted = pivotedSales
                .SelectMany(p => new[]
                {
                    (p.EmployeeId, "Jan", p.Jan),
                    (p.EmployeeId, "Feb", p.Feb),
                    (p.EmployeeId, "Mar", p.Mar)
                })
                .ToList();

            foreach (var row in unpivoted)
            {
                Console.WriteLine($"EmployeeId: {row.EmployeeId}, Month: {row.Item2}, Sales: {row.Item3}");
            }
        }
    }

    // Example EF DbContext
    public class MyDbContext
    {
        public DbSet<EmployeeSalePivot> EmployeeSalesPivot { get; set; } = new DbSet<EmployeeSalePivot>();
    }

    public class EmployeeSalePivot
    {
        public int EmployeeId { get; set; }
        public decimal Jan { get; set; }
        public decimal Feb { get; set; }
        public decimal Mar { get; set; }
    }

    // Mock DbSet for demonstration
    public class DbSet<T> : System.Collections.Generic.List<T>
    {
        public Task<System.Collections.Generic.List<T>> ToListAsync() => Task.FromResult(this.ToList());
    }
}
