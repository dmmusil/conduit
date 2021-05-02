using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Eventuous;
using Eventuous.Projections.MongoDB.Tools;
using MongoDB.Driver;

namespace Conduit.Api.Features.Accounts
{
    public record UserRegistration(
        string Email,
        string Username,
        string Password);

    public record UserLogin(string Email, string Password);

    public record UserUpdate(
        string? Email = null,
        string? Username = null,
        string? Password = null,
        string? Bio = null,
        string? Image = null);

    public record User(
        string Id,
        string Email,
        string Username,
        string? Bio = null,
        string? Image = null,
        string? Token = null);


    public static class Commands
    {
        public record Register(UserRegistration User);

        public record LogIn(string? StreamId, UserLogin User);

        public record UpdateUser(string? StreamId, UserUpdate User);

        public record FollowUser(string StreamId, string FollowedId);

        public record UnfollowUser(string StreamId, string FollowedId);
    }

    public static class Events
    {
        public record UserRegistered(
            string StreamId,
            string Email,
            string Username,
            string PasswordHash);

        public record UsernameUpdated(string StreamId, string Username);

        public record PasswordUpdated(string StreamId, string PasswordHash);

        public record BioUpdated(string StreamId, string Bio);

        public record ImageUpdated(string StreamId, string Image);

        public record EmailUpdated(string StreamId, string Email);

        public record AccountFollowed(string StreamId, string FollowedId);

        public record AccountUnfollowed(string StreamId, string UnfollowedId);

        public static void Register()
        {
            TypeMap.AddType<UserRegistered>(nameof(UserRegistered));
            TypeMap.AddType<UsernameUpdated>(nameof(UsernameUpdated));
            TypeMap.AddType<PasswordUpdated>(nameof(PasswordUpdated));
            TypeMap.AddType<BioUpdated>(nameof(BioUpdated));
            TypeMap.AddType<ImageUpdated>(nameof(ImageUpdated));
            TypeMap.AddType<EmailUpdated>(nameof(EmailUpdated));
            TypeMap.AddType<AccountFollowed>(nameof(AccountFollowed));
            TypeMap.AddType<AccountUnfollowed>(nameof(AccountUnfollowed));
        }
    }

    public static class Projections
    {
        public record UserDocument(
            string StreamId,
            string Email,
            string Username,
            string PasswordHash,
            string? Bio,
            string? Image,
            IEnumerable<string> Following) : ProjectedDocument(StreamId)
        {
            public bool VerifyPassword(string password) =>
                BCrypt.Net.BCrypt.Verify(password, PasswordHash);

            public bool IsFollowing(string authorId) =>
                Following.Contains(authorId);
        }
    }

    public class
        UserService : ApplicationService<Account, AccountState, AccountId>
    {
        private readonly IAggregateStore _store;

        public UserService(IAggregateStore store) : base(store)
        {
            _store = store;
            OnNew<Commands.Register>(
                (account, cmd) =>
                {
                    var hashedPassword =
                        BCrypt.Net.BCrypt.HashPassword(cmd.User.Password);
                    account.Register(
                        cmd.User.Username,
                        cmd.User.Email,
                        hashedPassword);
                });
            OnExisting<Commands.UpdateUser>(
                cmd => new AccountId(cmd.StreamId!),
                (account, cmd) =>
                {
                    var (email, username, password, bio, image) = cmd.User;
                    account.Update(email, username, password, bio, image);
                });
            OnExisting<Commands.FollowUser>(
                cmd => new AccountId(cmd.StreamId),
                (account, cmd) => account.Follow(cmd.FollowedId));
            OnExisting<Commands.UnfollowUser>(
                cmd => new AccountId(cmd.StreamId),
                (account, cmd) => account.Unfollow(cmd.FollowedId));
        }

        public async Task<User> Load(string userId)
        {
            var account = await _store.Load<Account>(new AccountId(userId));
            var state = account.State;
            return new User(userId, state.Email, state.Username);
        }
    }

    public record AccountId(string Value) : AggregateId(Value);

    public record AccountState : AggregateState<AccountState, AccountId>
    {
        public override AccountState When(object @event)
        {
            return @event switch
            {
                Events.UserRegistered(var streamId, var email, var username, var
                    passwordHash) => this with
                    {
                        Id = new AccountId(streamId),
                        Email = email,
                        Username = username,
                        PasswordHash = passwordHash
                    },
                Events.EmailUpdated(_, var email) => this with {Email = email},
                Events.UsernameUpdated(_, var username) => this with
                {
                    Username = username
                },
                Events.BioUpdated(_, var bio) => this with {Bio = bio},
                Events.PasswordUpdated(_, var passwordHash) => this with
                {
                    PasswordHash = passwordHash
                },
                Events.ImageUpdated(_, var image) => this with {Image = image},
                Events.AccountFollowed e => this with
                {
                    FollowedProfiles = FollowedProfiles.Add(e.FollowedId)
                },
                Events.AccountUnfollowed e => this with
                {
                    FollowedProfiles = FollowedProfiles.Remove(e.UnfollowedId)
                },
                _ => throw new ArgumentOutOfRangeException(
                    nameof(@event),
                    "Unknown event")
            };
        }

        public string PasswordHash { get; private init; } = null!;
        public string Email { get; private init; } = null!;
        public string Username { get; private init; } = null!;
        public string? Bio { get; private init; }
        public string? Image { get; private init; }

        private ImmutableHashSet<string> FollowedProfiles { get; init; } =
            ImmutableHashSet<string>.Empty;

        public bool AlreadyRegistered => Id != null;

        public bool AlreadyFollowing(string followedId) =>
            FollowedProfiles.Contains(followedId);

        public bool NotFollowing(string followedId) =>
            !AlreadyFollowing(followedId);
    }

    public class Account : Aggregate<AccountState, AccountId>
    {
        public void Register(string username, string email, string passwordHash)
        {
            if (State.AlreadyRegistered) return;
            Apply(
                new Events.UserRegistered(
                    Guid.NewGuid().ToString("N"),
                    email,
                    username,
                    passwordHash));
        }

        public void Update(
            string? email,
            string? username,
            string? password,
            string? bio,
            string? image)
        {
            EnsureExists();
            if (email != null)
                Apply(new Events.EmailUpdated(State.Id, email));
            if (username != null)
                Apply(new Events.UsernameUpdated(State.Id, username));
            if (password != null)
                Apply(
                    new Events.PasswordUpdated(
                        State.Id,
                        BCrypt.Net.BCrypt.HashPassword(password)));
            if (bio != null)
                Apply(new Events.BioUpdated(State.Id, bio));
            if (image != null)
                Apply(new Events.ImageUpdated(State.Id, image));
        }

        public void Follow(string followedId)
        {
            EnsureExists();
            if (State.AlreadyFollowing(followedId)) return;
            Apply(new Events.AccountFollowed(State.Id, followedId));
        }

        public void Unfollow(string followedId)
        {
            EnsureExists();
            if (State.NotFollowing(followedId)) return;
            Apply(new Events.AccountUnfollowed(State.Id, followedId));
        }
    }

    public class UserRepository
    {
        private readonly IMongoCollection<Projections.UserDocument> _database;

        public UserRepository(IMongoDatabase database) =>
            _database =
                database.GetCollection<Projections.UserDocument>("User");

        public async Task<Projections.UserDocument> GetUserByEmail(string email)
        {
            var query = await _database.FindAsync(d => d.Email == email);
            return await query.SingleOrDefaultAsync();
        }

        public async Task<Projections.UserDocument> GetUserByUsername(
            string username)
        {
            var query = await _database.FindAsync(d => d.Username == username);
            return await query.SingleOrDefaultAsync();
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

        public async Task<Projections.UserDocument> GetUserByUuid(string uuid)
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