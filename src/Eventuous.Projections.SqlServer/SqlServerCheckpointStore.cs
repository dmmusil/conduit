using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Eventuous.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Eventuous.Projections.SqlServer
{
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
    StreamId        varchar(200)    not null,
    Email           varchar(200)    not null,
    Username        varchar(50)     not null,
    PasswordHash    varchar(200)    not null,
    Bio             varchar(1000)   null,
    Image           varchar(200)    null,
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