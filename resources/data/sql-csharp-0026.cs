using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RunStoredProcedureSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";
            
            // ❌ VIOLATION: Direct stored procedure execution via SqlCommand
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("GetUsers", connection);
                cmd.CommandType = CommandType.StoredProcedure; // Direct stored procedure call
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine(reader["UserName"]);
                }
            }

            // ❌ VIOLATION: Custom wrapper method for stored procedure execution
            RunStoredProcedure("GetOrders");

            // ✅ COMPLIANT: Using ORM/Repository abstraction instead of direct stored procedure calls
            var repo = new UserRepository();
            var users = repo.GetAllUsers();
            foreach (var user in users)
            {
                Console.WriteLine(user);
            }

            // ✅ COMPLIANT: Async-friendly query execution using ORM abstraction
            var asyncRepo = new UserRepository();
            Task.Run(async () =>
            {
                var asyncUsers = await asyncRepo.GetAllUsersAsync();
                foreach (var u in asyncUsers)
                {
                    Console.WriteLine(u);
                }
            }).Wait();
        }

        // ❌ Custom stored procedure runner (to be flagged)
        static void RunStoredProcedure(string procedureName)
        {
            using (SqlConnection conn = new SqlConnection("Server=.;Database=TestDb;Trusted_Connection=True;"))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(procedureName, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ✅ Compliant Repository pattern (ORM abstraction)
    class UserRepository
    {
        public string[] GetAllUsers()
        {
            // Pretend ORM fetch
            return new[] { "Alice", "Bob" };
        }

        public Task<string[]> GetAllUsersAsync()
        {
            // Pretend async ORM fetch
            return Task.FromResult(new[] { "Charlie", "Diana" });
        }
    }
}
