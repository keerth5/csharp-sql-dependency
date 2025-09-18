using System;
using System.Data;
using System.Data.SqlClient; // direct DB usage
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CommandTypePropertyExample
{
    // EF Core DbContext (Compliant ORM abstraction)
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CommandTypePropertyTest
    {
        private readonly string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        public void Bad_Use_CommandType_Text()
        {
            // ❌ Violation: Explicitly setting CommandType to Text
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT * FROM Employees", connection))
                {
                    command.CommandType = CommandType.Text; // violation
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Name"]);
                        }
                    }
                }
            }
        }

        public void Bad_Use_CommandType_StoredProcedure()
        {
            // ❌ Violation: Explicitly setting CommandType to StoredProcedure
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("GetAllEmployees", connection))
                {
                    command.CommandType = CommandType.StoredProcedure; // violation
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["Name"]);
                        }
                    }
                }
            }
        }

        public async Task Good_Use_EntityFramework_Linq_Async()
        {
            // ✅ Compliant: ORM query via LINQ (no CommandType required)
            using (var context = new AppDbContext())
            {
                var employees = await context.Employees.ToListAsync();
                foreach (var e in employees)
                {
                    Console.WriteLine(e.Name);
                }
            }
        }

        public async Task Good_Use_EntityFramework_FromSqlRaw_Async()
        {
            // ✅ Compliant: ORM abstraction for raw SQL, no explicit CommandType
            using (var context = new AppDbContext())
            {
                var employees = await context.Employees
                    .FromSqlRaw("EXEC GetAllEmployees")
                    .ToListAsync();

                foreach (var e in employees)
                {
                    Console.WriteLine(e.Name);
                }
            }
        }
    }
}
