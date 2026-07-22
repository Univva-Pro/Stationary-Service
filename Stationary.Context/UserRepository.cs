using System.Threading.Tasks;
using Stationary.DMO;
using MongoDB.Driver;

namespace Stationary.Context
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _users = database.GetCollection<User>("users");
            EnsureUsersSeeded();
        }

        private void EnsureUsersSeeded()
        {
            try 
            {
                if (_users.CountDocuments(Builders<User>.Filter.Empty) == 0)
                {
                    _users.InsertMany(new[]
                    {
                        new User { Username = "admin", PasswordHash = "admin@05", Role = "Admin" },
                        new User { Username = "user", PasswordHash = "user123", Role = "User" }
                    });
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[DB SEED ERROR] User seed failed: {ex.Message}");
            }
        }

        public async Task<User?> GetUserAsync(string username, string passwordHash)
        {
            return await _users.Find(u => u.Username == username && u.PasswordHash == passwordHash).FirstOrDefaultAsync();
        }
    }
}
