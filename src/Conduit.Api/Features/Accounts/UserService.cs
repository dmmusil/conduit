using System.Threading;
using System.Threading.Tasks;
using Conduit.Api.Features.Accounts.Aggregates;
using Eventuous;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.Extensions.Logging;

namespace Conduit.Api.Features.Accounts
{
    public class
        UserService : ImmediatelyConsistentApplicationService<Account, AccountState, AccountId>
    {
        private readonly IAggregateStore _store;
        private readonly StreamNameMap _map;

        public UserService(
            IAggregateStore store,
            StreamNameMap map,
            ICheckpointStore checkpointStore,
            ILoggerFactory loggerFactory) : base(store,
            checkpointStore, loggerFactory)
        {
            _store = store;
            _map = map;
            OnNew<Commands.Register>(
                cmd => new AccountId(cmd.User.Id),
                (account, cmd) =>
                {
                    var hashedPassword =
                        BCrypt.Net.BCrypt.HashPassword(cmd.User.Password);
                    account.Register(
                        new AccountId(cmd.User.Id),
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
            var account = await _store.Load<Account>(
                _map.GetStreamName<Account, AccountId>(new AccountId(userId)),
                CancellationToken.None);
            var state = account.State;
            return new User(userId, state.Email, state.Username, state.Bio, state.Image);
        }
    }
}