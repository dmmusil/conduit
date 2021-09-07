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
        private readonly ILogger _log;

        protected SqlServerProjection(IConfiguration configuration,
            string subscriptionId, ILoggerFactory loggerFactory)
        {
            SubscriptionId = subscriptionId;
            _connectionString = configuration.GetConnectionString("ReadModels");
            _log = loggerFactory.CreateLogger(GetType());
        }

        public async Task HandleEvent(object evt, long? position)
        {
            await using var connection = new SqlConnection(_connectionString);
            var commandDefinition = GetCommand(evt);
            if (string.IsNullOrEmpty(commandDefinition.CommandText))
            {
                _log.LogDebug($"No handler for {evt.GetType().Name}");
                return;
            }

            _log.LogDebug($"Projecting {evt.GetType().Name}. {commandDefinition.CommandText}");
            await connection.ExecuteAsync(commandDefinition);
        }

        protected abstract CommandDefinition GetCommand(object evt);

        public Task HandleEvent(object evt, long? position,
            CancellationToken cancellationToken) => HandleEvent(evt, position);

        public string SubscriptionId { get; }
    }
}