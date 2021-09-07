using System.Data;
using System.Threading.Tasks;
using Conduit.Api.Features.Accounts.Projections;
using Dapper;
using MongoDB.Driver;

namespace Conduit.Api.Features.Accounts.Queries
{
    public class UserRepository
    {
        private readonly IDbConnection _connection;
        private readonly IMongoCollection<UserDocument> _database;

        public UserRepository(IMongoDatabase database, IDbConnection connection)
        {
            _connection = connection;
            _database = database.GetCollection<UserDocument>("User");
        }

        public async Task<UserDocument> GetUserByEmail(string email)
        {
            var query = await _database.FindAsync(d => d.Email == email);
            return await query.SingleOrDefaultAsync();
        }

        public async Task<User> GetUserByUsername(string username)
        {
            const string query =
                "select StreamId as Id, Email, Username, Bio, Image from Accounts where Username=@username";
            return await _connection.QuerySingleOrDefaultAsync<User>(
                query, new {username});
        }

        public async Task<bool> UsernameExists(
            string? username,
            User? user = null)
        {
            if (username == null) return false;
            var userByUsername = await GetUserByUsername(username);
            return userByUsername != null && userByUsername.Id != user?.Id;
        }

        public async Task<bool> EmailExists(string? email, User? user = null)
        {
            if (email == null) return false;
            var userWithEmail = await GetUserByEmail(email);
            return userWithEmail != null && userWithEmail.Id != user?.Id;
        }

        public async Task<UserDocument> GetUserByUuid(string uuid)
        {
            var query = await _database.FindAsync(d => d.Id == uuid);
            return await query.SingleOrDefaultAsync();
        }

        public async Task<Profile?> ProfileWithFollowingStatus(
            string username,
            string? callerId)
        {
            var profile = await GetUserByUsername(username);
            if (profile == null) return null;

            if (callerId == null)
                return new Profile(
                    profile.Username,
                    profile.Bio,
                    profile.Image,
                    false);

            var callerProfile = await GetUserByUuid(callerId);
            return new Profile(
                profile.Username,
                profile.Bio,
                profile.Image,
                callerProfile.IsFollowing(profile.Id));
        }
    }
}