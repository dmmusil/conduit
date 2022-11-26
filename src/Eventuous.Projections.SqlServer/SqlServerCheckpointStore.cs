using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Eventuous.Projections.SqlServer
{
    public class SqlServerCheckpointStore : ICheckpointStore
    {
        private readonly ISchemaManagement _manager;
        private readonly string _connectionString;

        public SqlServerCheckpointStore(IConfiguration config, ISchemaManagement manager)
        {
            _manager = manager;
            _connectionString = config.GetConnectionString("ReadModels");
        }

        public async ValueTask<Checkpoint> GetLastCheckpoint(
            string checkpointId,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await using var connection = new SqlConnection(_connectionString);

            await _manager.CreateSchemaOnce();

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
            bool force,
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

    public interface ISchemaManagement
    {
        Task CreateSchemaOnce();
    }
}