using System;
using System.Data.Common;
using Conduit.Api.Features.Accounts.Events;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace Conduit.Api.Features.Accounts.Projectors
{
    public class SqlAccountsEventHandler : SqlServerProjection
    {
        public SqlAccountsEventHandler(
            IConfiguration configuration,
            string subscriptionId) : base(configuration, subscriptionId)
        {
        }

        protected override CommandDefinition GetCommand(object evt)
        {
            return evt switch
            {
                Events.UserRegistered e => new UserRegisteredCommand(e)
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