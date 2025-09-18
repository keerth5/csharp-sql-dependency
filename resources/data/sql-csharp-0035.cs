using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace SampleApp
{
    public class DapperConnectionQueryMethodExample
    {
        private readonly string _connectionString = "Server=.;Database=TestDb;Trusted_Connection=True;";

        // ❌ Violation: Using connection.Query directly without proper lifecycle management
        public void Bad_ConnectionQueryUsage()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var users = connection.Query<User>("SELECT * FROM Users");
                foreach (var user in users)
                {
                    Console.WriteLine($"User: {user.Name}");
                }
            }
        }

        // ❌ Violation: Synchronous direct query with open connection
        public void Bad_ConnectionQuerySync()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            var users = connection.Query<User>("SELECT * FROM Users WHERE IsActive = 1");
            Console.WriteLine($"Count: {users.AsList().Count}");
            connection.Close(); // risky if exception occurs before this
        }

        // ✅ Compliant: Using QueryAsync with proper connection lifecycle
        public async Task Good_ConnectionQueryUsageAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var users = await connection.QueryAsync<User>("SELECT * FROM Users WHERE IsActive = 1");
                foreach (var user in users)
                {
                    Console.WriteLine($"Active User: {user.Name}");
                }
            }
        }

        // ✅ Compliant: Encapsulating connection management inside repository
        public async Task Good_EncapsulatedConnectionQuery()
        {
            var repository = new UserRepository(_connectionString);
            var users = await repository.GetActiveUsersAsync();
            foreach (var user in users)
            {
                Console.WriteLine($"Repo User: {user.Name}");
            }
        }
    }

    public class UserRepository
    {
        private readonly string _connectionString;
        public UserRepository(string connectionString) => _connectionString = connectionString;

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<User>("SELECT * FROM Users WHERE IsActive = 1");
            }
        }
    }

    public class User
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
