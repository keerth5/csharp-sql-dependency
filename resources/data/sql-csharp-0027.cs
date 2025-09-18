using System;
using System.Threading.Tasks;

namespace SaveDataMethodSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = new { Id = 1, Name = "Test" };

            // ❌ VIOLATION: Direct synchronous SaveData method
            DataSaver.SaveData(data);

            // ❌ VIOLATION: Custom SaveData wrapper method
            SaveData(data);

            // ✅ COMPLIANT: Async data persistence via ORM or repository
            var repo = new UserRepository();
            Task.Run(async () =>
            {
                await repo.SaveUserAsync(data);
                Console.WriteLine("User saved asynchronously.");
            }).Wait();
        }

        // ❌ Custom SaveData implementation (to be flagged)
        static void SaveData(object obj)
        {
            Console.WriteLine("Data saved synchronously: " + obj);
        }
    }

    // ❌ Legacy synchronous saver (to be flagged)
    class DataSaver
    {
        public static void SaveData(object obj)
        {
            Console.WriteLine("Legacy SaveData called with: " + obj);
        }
    }

    // ✅ Modern async repository pattern
    class UserRepository
    {
        public async Task SaveUserAsync(object user)
        {
            await Task.Delay(100); // Simulate async DB operation
            Console.WriteLine("User saved via repository async method: " + user);
        }
    }
}
