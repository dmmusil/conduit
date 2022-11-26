using System.Collections.Generic;
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
            IConfiguration configuration, ILoggerFactory loggerFactory) : base(
            configuration, loggerFactory)
        {
        }

        protected override IEnumerable<CommandDefinition> GetCommand(object evt)
        {
            var command = evt switch
            {
                UserRegistered e => new UserRegisteredInsert(e)
                    .CommandDefinition,
                EmailUpdated e => new CommandDefinition("update Accounts set Email=@Email where StreamId=@StreamId", e),
                PasswordUpdated e => new CommandDefinition("update Accounts set PasswordHash=@PasswordHash where StreamId=@StreamId", e),
                BioUpdated e => new CommandDefinition("update Accounts set Bio=@Bio where StreamId=@StreamId", e),
                ImageUpdated e => new CommandDefinition("update Accounts set Image=@Image where StreamId=@StreamId", e),
                UsernameUpdated e => new CommandDefinition("update Accounts set Username=@Username where StreamId=@StreamId", e),
                AccountFollowed e => new CommandDefinition("insert into Followers (FollowedUserId, FollowingUserId) values (@FollowedId, @StreamId)", e),
                AccountUnfollowed e => new CommandDefinition("delete from Followers where FollowedUserId=@UnfollowedId and FollowingUserId=@StreamId", e),
                _ => default
            };

            return ArrayOf(command);
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