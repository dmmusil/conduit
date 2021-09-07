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
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public SqlServerCheckpointStore(IConfiguration config)
        {
            _config = config;
            _connectionString = config.GetConnectionString("ReadModels");
        }

        public async ValueTask<Checkpoint> GetLastCheckpoint(
            string checkpointId,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await using var connection = new SqlConnection(_connectionString);

            await new SchemaManagement(_config).CreateSchemaOnce();
            
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