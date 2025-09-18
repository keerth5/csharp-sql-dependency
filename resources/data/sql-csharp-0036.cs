using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    public class DynamicSqlConstructionExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Building SQL dynamically using StringBuilder
        public void Bad_DynamicSqlWithStringBuilder(string userInput)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM Users WHERE Name = '");
            sb.Append(userInput); // dangerous concatenation
            sb.Append("'");

            var sql = sb.ToString();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}");
                }
            }
        }

        // ❌ Violation: Dynamic filtering with StringBuilder
        public void Bad_DynamicSqlMultipleConditions(string role, int age)
        {
            var sb = new StringBuilder("SELECT * FROM Users WHERE 1=1 ");
            sb.Append("AND Role = '" + role + "' ");
            sb.Append("AND Age > " + age);

            var sql = sb.ToString();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["Role"]}");
                }
            }
        }

        // ✅ Compliant: Using parameterized query
        public async Task Good_ParameterizedQuery(string userInput)
        {
            var sql = "SELECT * FROM Users WHERE Name = @Name";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Name", userInput);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}");
                }
            }
        }

        // ✅ Compliant: Safe filtering with parameters
        public async Task Good_FilteringWithParameters(string role, int age)
        {
            var sql = "SELECT * FROM Users WHERE Role = @Role AND Age > @Age";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@Age", age);

                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"User: {reader["Name"]}, Role: {reader["Role"]}");
                }
            }
        }
    }
}
