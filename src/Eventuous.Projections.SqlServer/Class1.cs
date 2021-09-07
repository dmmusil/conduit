using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using EventStore.Client;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.EventStoreDB;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreamSubscription = EventStore.Client.StreamSubscription;

namespace Eventuous.Projections.SqlServer
{
    public class
        TransactionalAllStreamSubscriptionService :
            EventStoreSubscriptionService
    {
        private readonly ILogger _log;

        public TransactionalAllStreamSubscriptionService(
            EventStoreClient eventStoreClient,
            EventStoreSubscriptionOptions options,
            ICheckpointStore checkpointStore,
            IEnumerable<IEventHandler> eventHandlers,
            IEventSerializer? eventSerializer = null,
            ILoggerFactory? loggerFactory = null,
            ISubscriptionGapMeasure? measure = null) : base(eventStoreClient,
            options, checkpointStore, eventHandlers, eventSerializer,
            loggerFactory, measure)
        {
            _log = loggerFactory.CreateLogger(GetType());
        }

        protected override async Task<EventSubscription> Subscribe(
            Checkpoint checkpoint,
            CancellationToken cancellationToken)
        {
            var filterOptions = new SubscriptionFilterOptions(
                EventTypeFilter.ExcludeSystemEvents(),
                10,
                (_, p, ct) =>
                    StoreCheckpoint(
                        new EventPosition(p.CommitPosition, DateTime.UtcNow),
                        ct));

            var (_, position) = checkpoint;
            var subscribeTask = position != null
                ? EventStoreClient.SubscribeToAllAsync(
                    new Position(
                        position.Value,
                        position.Value),
                    TransactionalHandler,
                    false,
                    HandleDrop,
                    filterOptions,
                    cancellationToken: cancellationToken)
                : EventStoreClient.SubscribeToAllAsync(
                    TransactionalHandler,
                    false,
                    HandleDrop,
                    filterOptions,
                    cancellationToken: cancellationToken);

            var sub = await subscribeTask.NoContext();

            return new EventSubscription(SubscriptionId,
                new Stoppable(() => sub.Dispose()));
        }

        private void HandleDrop(
            StreamSubscription arg1,
            SubscriptionDroppedReason arg2,
            Exception? arg3)
        {
        }

        private async Task TransactionalHandler(
            StreamSubscription _,
            ResolvedEvent e,
            CancellationToken ct)
        {
            _log.LogDebug($"Subscription {SubscriptionId} got an event {e.Event.EventType}");
            using var tx = new TransactionScope();

            await Handler(AsReceivedEvent(e), ct);

            tx.Complete();

            ReceivedEvent AsReceivedEvent(ResolvedEvent re)
            {
                var evt = DeserializeData(
                    re.Event.ContentType,
                    re.Event.EventType,
                    re.Event.Data,
                    re.Event.EventStreamId,
                    re.Event.EventNumber
                );

                return new ReceivedEvent(
                    re.Event.EventId.ToString(),
                    re.Event.EventType,
                    re.Event.ContentType,
                    re.Event.Position.CommitPosition,
                    re.Event.Position.CommitPosition,
                    re.OriginalStreamId,
                    re.Event.EventNumber,
                    re.Event.Created,
                    evt
                    // re.Event.Metadata
                );
            }
        }
    }

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


    public class SqlServerCheckpointStore : ICheckpointStore
    {
        private readonly string _connectionString;
        private readonly string _masterConnectionString;

        public SqlServerCheckpointStore(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("ReadModels");
            _masterConnectionString = config.GetConnectionString("Master");
        }

        public async ValueTask<Checkpoint> GetLastCheckpoint(
            string checkpointId,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await using var connection = new SqlConnection(_connectionString);

            await EnsureDatabaseOnce();

            const string query = @"
            select Position 
            from Checkpoints 
            where Id=@Id";

            var result = await connection.QuerySingleOrDefaultAsync<long>(
                query,
                new { Id = checkpointId });
            return result == default
                ? new Checkpoint(checkpointId, null)
                : new Checkpoint(checkpointId, (ulong?)result);
        }

        private static bool _dbExists;

        private async Task EnsureDatabaseOnce()
        {
            if (_dbExists) return;

            await EnsureDatabase();
            await EnsureCheckpoints();
            await EnsureAccounts();

            _dbExists = true;
        }

        private async Task EnsureAccounts()
        {
            const string query = @"
if exists(select *
          from conduit.INFORMATION_SCHEMA.TABLES
          where TABLE_NAME = 'Accounts')
    begin
        set noexec on;
    end

create table dbo.Accounts
(
    StreamId       varchar(200) not null,
    Email          varchar(200) not null,
    Username       varchar(50)  not null,
    PasswordHash          varchar(200) not null,
    constraint PK_Accounts primary key (StreamId)
)
";
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query);
            Console.WriteLine("Created accounts.");
        }

        private async Task EnsureDatabase()
        {
            const string query = @"
if exists(select *
          from sys.databases
          where name = 'conduit')
    begin
        set noexec on;
    end

create database conduit;

set noexec off;

";
            await using var masterConnection =
                new SqlConnection(_masterConnectionString);

            await TryConnect(masterConnection);

            await masterConnection.ExecuteAsync(query);
            Console.WriteLine("Created database.");
        }

        private static async Task TryConnect(IDbConnection masterConnection)
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    await masterConnection.QueryAsync("select 1");
                }
                catch
                {
                    Console.WriteLine($"Login attempt {i} failed.");
                    await Task.Delay(1000);
                }
            }
        }

        private async Task EnsureCheckpoints()
        {
            const string query = @"
if exists(select *
          from conduit.INFORMATION_SCHEMA.TABLES
          where TABLE_NAME = 'Checkpoints')
    begin
        set noexec on;
    end

create table dbo.Checkpoints
(
    Id       varchar(200) not null,
    Position bigint       null,
    constraint PK_Checkpoints primary key (Id)
)
";
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query);
            Console.WriteLine("Created checkpoints table.");
        }

        public async ValueTask<Checkpoint> StoreCheckpoint(
            Checkpoint checkpoint,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await using var connection = new SqlConnection(_connectionString);
            const string query = @"
update Checkpoints
set Position=@Position
where Id=@Id

if @@ROWCOUNT=0
begin
    insert into Checkpoints (Id, Position)
    values (@Id, @Position)
end
";
            await connection.ExecuteAsync(
                query,
                new { checkpoint.Id, Position = (long?)checkpoint.Position });
            return checkpoint;
        }
    }
}