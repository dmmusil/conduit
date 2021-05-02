using System.Threading.Tasks;
using Eventuous.Projections.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Conduit.Api.Features.Accounts.Projectors
{
    public class
        AccountsEventHandler : MongoProjection<Projections.UserDocument>
    {
        public AccountsEventHandler(
            IMongoDatabase database,
            string subscriptionGroup,
            ILoggerFactory loggerFactory) : base(
            database,
            subscriptionGroup,
            loggerFactory)
        {
        }

        protected override ValueTask<Operation<Projections.UserDocument>>
            GetUpdate(object evt)
        {
            return evt switch
            {
                Events.UserRegistered(var streamId, var email, var username, var
                    passwordHash) => UpdateOperationTask(
                        streamId,
                        update => update.Set(d => d.Email, email)
                            .Set(d => d.Username, username)
                            .Set(d => d.PasswordHash, passwordHash)
                            .Set(
                                d => d.Following,
                                System.Array.Empty<string>())),
                Events.EmailUpdated (var streamId, var email) =>
                    UpdateOperationTask(
                        streamId,
                        u => u.Set(d => d.Email, email)),
                Events.UsernameUpdated (var streamId, var username) =>
                    UpdateOperationTask(
                        streamId,
                        u => u.Set(d => d.Username, username)),
                Events.PasswordUpdated (var streamId, var passwordHash) =>
                    UpdateOperationTask(
                        streamId,
                        u => u.Set(d => d.PasswordHash, passwordHash)),
                Events.BioUpdated (var streamId, var bio) =>
                    UpdateOperationTask(streamId, u => u.Set(d => d.Bio, bio)),
                Events.ImageUpdated (var streamId, var image) =>
                    UpdateOperationTask(
                        streamId,
                        u => u.Set(d => d.Image, image)),
                Events.AccountFollowed(var streamId, var followedId) =>
                    UpdateOperationTask(
                        streamId,
                        u => u.AddToSet(d => d.Following, followedId)),
                Events.AccountUnfollowed(var streamId, var followedId) =>
                    UpdateOperationTask(
                        streamId,
                        u => u.Pull(d => d.Following, followedId)),
                _ => NoOp
            };
        }
    }
}