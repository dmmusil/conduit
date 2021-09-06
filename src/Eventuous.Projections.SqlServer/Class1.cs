using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using EventStore.Client;
using Eventuous.EventStoreDB.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eventuous.Projections.SqlServer
{
    public class TransactionalAllStreamSubscriptionService : SubscriptionService
    {
        protected override ulong? GetPosition(ResolvedEvent resolvedEvent) =>
            resolvedEvent.OriginalPosition?.CommitPosition;

        protected override async Task<StreamSubscription> Subscribe(
            Checkpoint checkpoint,
            CancellationToken cancellationToken)
        {
            var filterOptions = new SubscriptionFilterOptions(
                EventTypeFilter.ExcludeSystemEvents(),
                10,
                (_, p, ct) => StoreCheckpoint(p.CommitPosition, ct));

            var subscribeTask = checkpoint.Position != null
                ? EventStoreClient.SubscribeToAllAsync(
                    new Position(
                        checkpoint.Position.Value,
                        checkpoint.Position.Value),
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
        }

        private void HandleDrop(
            StreamSubscription arg1,
            SubscriptionDroppedReason arg2,
            Exception? arg3)
        {
        }

        private async Task TransactionalHandler(
            StreamSubscription arg1,
            ResolvedEvent arg2,
            CancellationToken arg3)
        {
            using var tx = new TransactionScope();

            await Handler(arg1, arg2, arg3);

            tx.Complete();
        }


        public TransactionalAllStreamSubscriptionService(
            EventStoreClient eventStoreClient,
            string subscriptionId,
            ICheckpointStore checkpointStore,
            IEventSerializer eventSerializer,
            IEnumerable<IEventHandler> eventHandlers,
            ILoggerFactory? loggerFactory = null,
            SubscriptionGapMeasure? measure = null) : base(
            eventStoreClient,
            subscriptionId,
            checkpointStore,
            eventSerializer,
            eventHandlers,
            loggerFactory,
            measure)
        {
        }
    }
}

public abstract class SqlServerProjection : IEventHandler
{
    private readonly string _connectionString;

    protected SqlServerProjection(IConfiguration configuration, string subscriptionId)
    {
        SubscriptionId = subscriptionId;
        _connectionString = configuration.GetConnectionString("ReadModels");
    }

    public async Task HandleEvent(object evt, long? position)
    {
        await using var connection = new SqlConnection(_connectionString);
        var commandDefinition = GetCommand(evt);
        if (string.IsNullOrEmpty(commandDefinition.CommandText)) return;
        await connection.ExecuteAsync(commandDefinition);
    }

    protected abstract CommandDefinition GetCommand(object evt);

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

        _dbExists = true;
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
        Console.WriteLine("Created schema.");
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