using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Conduit.Api.Features.Accounts.Queries
{
    public class UserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            const string query =
                "select StreamId as Id, Email, Username, Bio, Image from Accounts where Email=@email";
            return await _connection.QuerySingleOrDefaultAsync<User>(
                query, new {email});
        }

        public async Task<User?> GetUserByUsername(string username)
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

        public async Task<User> GetUserByUuid(string uuid)
        {
            const string query =
                "select StreamId as Id, Email, Username, Bio, Image from Accounts where StreamId=@uuid";
            return await _connection.QuerySingleOrDefaultAsync<User>(
                query, new {uuid});
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

            var isFollowing = await IsUserFollowing(callerId, profile.Id);
            
            return new Profile(
                profile.Username,
                profile.Bio,
                profile.Image,
                isFollowing);
        }

        private async Task<bool> IsUserFollowing(string callerId, string profileId)
        {
            const string query =
                "select FollowedUserId from Followers where FollowedUserId=@profileId and FollowingUserId=@callerId";
            var followedUser =
                await _connection.QuerySingleOrDefaultAsync<string>(query,
                    new { callerId, profileId });
            return followedUser != null;
        }

        public async Task<User?> Authenticate(string email, string password)
        {
            const string query =
                "select StreamId as Id, Email, Username, Bio, Image, PasswordHash from Accounts where Email=@email";
            var user = await _connection.QuerySingleOrDefaultAsync<dynamic>(
                query, new {email});
            var valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return valid
                ? new User(user.Id, user.Email, user.Username, user.Bio,
                    user.Image)
                : null;
        }
    }
}