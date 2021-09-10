using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Eventuous.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eventuous.Projections.SqlServer
{
    public abstract class SqlServerProjection : IEventHandler
    {
        private readonly string _connectionString;
        protected readonly ILogger _log;

        protected SqlServerProjection(IConfiguration configuration,
            string subscriptionId, ILoggerFactory loggerFactory)
        {
            SubscriptionId = subscriptionId;
            _connectionString = configuration.GetConnectionString("ReadModels");
            _log = loggerFactory.CreateLogger(GetType());
        }

        public async Task HandleEvent(object evt, long? position)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                var commands = GetCommand(evt);
                foreach (var commandDefinition in commands)
                {
                    if (string.IsNullOrEmpty(commandDefinition.CommandText))
                    {
                        _log.LogDebug("No handler for {Name}", evt.GetType().Name);
                        continue;
                    }

                    _log.LogDebug("Projecting {Name}. {CommandText} - {Evt}",
                        evt.GetType().Name, commandDefinition.CommandText, evt);
                    await connection.ExecuteAsync(commandDefinition);
                }
            }
            catch (Exception e)
            {
                _log.LogError(e.ToString());
                throw;
            }
        }

        protected abstract IEnumerable<CommandDefinition> GetCommand(object evt);

        protected static IEnumerable<CommandDefinition> ArrayOf(params CommandDefinition[] commands) => commands;
        public Task HandleEvent(object evt, long? position,
            CancellationToken cancellationToken) => HandleEvent(evt, position);

        public string SubscriptionId { get; }
    }
}