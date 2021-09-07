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
                UserRegistered e => new UserRegisteredInsert(e)
                    .CommandDefinition,
                EmailUpdated e => new CommandDefinition("update Accounts set Email=@Email where StreamId=@StreamId", e),
                PasswordUpdated e => new CommandDefinition("update Accounts set PasswordHash=@PasswordHash where StreamId=@StreamId", e),
                BioUpdated e => new CommandDefinition("update Accounts set Bio=@Bio where StreamId=@StreamId", e),
                ImageUpdated e => new CommandDefinition("update Accounts set Image=@Image where StreamId=@StreamId", e),
                UsernameUpdated e => new CommandDefinition("update Accounts set Username=@Username where StreamId=@StreamId", e),
                _ => default
            };
        }
    }

    
    
    public class UserRegisteredInsert
    {
        public UserRegisteredInsert(UserRegistered userRegistered)
        {
            CommandDefinition = new CommandDefinition(
                "insert into Accounts (StreamId, Email, Username, PasswordHash) values (@StreamId, @Email, @Username, @PasswordHash)",
                userRegistered);
        }

        public CommandDefinition CommandDefinition { get; }
    }
}