using System.Threading.Tasks;
using Conduit.Api.Features;
using Eventuous.Projections.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Conduit.Api.Infrastructure
{
    public class ConduitEventHandler : MongoProjection<Accounts.Projections.UserDocument>
    {
        private readonly ILogger<ConduitEventHandler> _loggerFactory;

        public ConduitEventHandler(
            IMongoDatabase database,
            string subscriptionGroup,
            ILoggerFactory loggerFactory)
            : base(
                database,
                subscriptionGroup,
                loggerFactory)
        {
            _loggerFactory = loggerFactory.CreateLogger<ConduitEventHandler>();
        }

        protected override ValueTask<Operation<Accounts.Projections.UserDocument>> GetUpdate(object evt)
        {
            _loggerFactory.LogInformation($"Processing {evt.GetType()}");
            return evt switch
            {
                Accounts.Events.UserRegistered(var streamId, var email, var username, var passwordHash)
                    => UpdateOperationTask(streamId,
                        update => update.Set(d => d.Email, email)
                            .Set(d => d.Username, username)
                            .Set(d => d.PasswordHash, passwordHash)),
                Accounts.Events.EmailUpdated (var streamId, var email) => UpdateOperationTask(streamId,
                    u => u.Set(d => d.Email, email)),
                Accounts.Events.UsernameUpdated (var streamId, var username) => UpdateOperationTask(streamId,
                    u => u.Set(d => d.Username, username)),
                Accounts.Events.PasswordUpdated (var streamId, var passwordHash) => UpdateOperationTask(streamId,
                    u => u.Set(d => d.PasswordHash, passwordHash)),
                Accounts.Events.BioUpdated (var streamId, var bio) => UpdateOperationTask(streamId,
                    u => u.Set(d => d.Bio, bio)),
                Accounts.Events.ImageUpdated (var streamId, var image) => UpdateOperationTask(streamId,
                    u => u.Set(d => d.Image, image)),
                _ => NoOp
            };
        }
    }
}