using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GetDataTableMethodExample
{
    // EF Core DbContext (Compliant ORM abstraction)
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // ❌ Example of a legacy GetDataTable helper
    public class LegacyDbHelper
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public DataTable GetDataTable(string sql) // violation target
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(connectionString))
            using (var adapter = new SqlDataAdapter(sql, connection))
            {
                adapter.Fill(table); // violation (legacy DataTable usage)
            }
            return table;
        }
    }

    public class GetDataTableTest
    {
        public void Bad_Use_GetDataTable()
        {
            // ❌ Violation: Using legacy GetDataTable() API
            var db = new LegacyDbHelper();
            DataTable dt = db.GetDataTable("SELECT * FROM Customers"); // violation
            foreach (DataRow row in dt.Rows)
            {
                Console.WriteLine(row["Name"]);
            }
        }

        public async Task Good_Use_EntityFramework_Async()
        {
            // ✅ Compliant: Modern EF Core async query
            using (var context = new AppDbContext())
            {
                var customers = await context.Customers.ToListAsync();
                foreach (var c in customers)
                {
                    Console.WriteLine(c.Name);
                }
            }
        }

        public async Task Good_Use_SqlDataReader_Async()
        {
            // ✅ Compliant: Use async SqlDataReader instead of DataTable
            var connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT Name FROM Customers", connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine(reader["Name"]);
                    }
                }
            }
        }
    }
}
