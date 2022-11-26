using System.Threading;
using System.Threading.Tasks;
using Eventuous;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.Extensions.Logging;

namespace Conduit.Api;

public class ImmediatelyConsistentApplicationService<T, TState, TId> : ApplicationService<T, TState, TId>
    where T : Aggregate<TState>, new()
    where TState : AggregateState<TState>, new()
    where TId : AggregateId
{
    private readonly ICheckpointStore _checkpointStore;
    private readonly ILogger _log;

    protected ImmediatelyConsistentApplicationService(
        IAggregateStore store,
        ICheckpointStore checkpointStore,
        ILoggerFactory loggerFactory,
        AggregateFactoryRegistry? factoryRegistry = null,
        StreamNameMap? streamNameMap = null) : base(store,
        factoryRegistry, streamNameMap)
    {
        _checkpointStore = checkpointStore;
        _log = loggerFactory.CreateLogger(GetType());
    }

    public async Task<Result<TState>> HandleImmediate(
        object command,
        string requiredReadModel = "ConduitSql",
        CancellationToken ct = default)
    {
        var result = await Handle(command, ct);
        if (result is not OkResult<TState> ok) return result;

        var checkpoint = await _checkpointStore.GetLastCheckpoint(requiredReadModel, ct);
        while (checkpoint.Position < ok.StreamPosition)
        {
            checkpoint = await _checkpointStore.GetLastCheckpoint(requiredReadModel, ct);
            _log.LogDebug($"Checkpoint: {checkpoint.Position}, stream position: {ok.StreamPosition}");
            await Task.Delay(10, ct);
        }

        return ok;
    }
}