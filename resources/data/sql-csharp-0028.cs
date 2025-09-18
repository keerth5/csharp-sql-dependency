using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FillDatasetMethodSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

            // ❌ VIOLATION: Using legacy DataSet population via DataAdapter.Fill
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Users", connection);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adapter.Fill(ds); // FillDataset pattern
                Console.WriteLine("Dataset filled with rows: " + ds.Tables[0].Rows.Count);
            }

            // ❌ VIOLATION: Custom wrapper method around dataset filling
            var ds2 = FillDataset("SELECT * FROM Orders");
            Console.WriteLine("Custom FillDataset wrapper, rows: " + ds2.Tables[0].Rows.Count);

            // ✅ COMPLIANT: ORM/Repository abstraction (no DataSet)
            var repo = new UserRepository();
            var users = repo.GetAllUsers();
            foreach (var user in users)
            {
                Console.WriteLine("User: " + user);
            }

            // ✅ COMPLIANT: Async ORM abstraction (scalable and modern)
            Task.Run(async () =>
            {
                var asyncUsers = await repo.GetAllUsersAsync();
                foreach (var u in asyncUsers)
                {
                    Console.WriteLine("Async User: " + u);
                }
            }).Wait();
        }

        // ❌ Custom legacy wrapper (to be flagged)
        static DataSet FillDataset(string query)
        {
            using (SqlConnection connection = new SqlConnection("Server=.;Database=TestDb;Trusted_Connection=True;"))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                return ds;
            }
        }
    }

    // ✅ Repository abstraction replacing DataSet usage
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
