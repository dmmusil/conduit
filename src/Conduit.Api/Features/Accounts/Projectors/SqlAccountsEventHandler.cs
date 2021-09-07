using Conduit.Api.Features.Accounts.Events;
using Dapper;
using Eventuous.Projections.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Conduit.Api.Features.Accounts.Projectors
{
    public class SqlAccountsEventHandler : SqlServerProjection
    {
        public SqlAccountsEventHandler(
            IConfiguration configuration,
            string subscriptionId, ILoggerFactory loggerFactory) : base(
            configuration, subscriptionId, loggerFactory)
        {
        }

        protected override CommandDefinition GetCommand(object evt)
        {
            return evt switch
            {
                UserRegistered e => new UserRegisteredCommand(e)
                    .CommandDefinition,
                _ => default
            };
        }
    }

    public class UserRegisteredCommand
    {
        public UserRegisteredCommand(UserRegistered userRegistered)
        {
            CommandDefinition = new CommandDefinition(
                "insert into Accounts (StreamId, Email, Username, PasswordHash) values (@StreamId, @Email, @Username, @PasswordHash)",
                userRegistered);
        }

        public CommandDefinition CommandDefinition { get; }
    }
}