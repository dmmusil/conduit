using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventStore.Client;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Checkpoints;
using Eventuous.Subscriptions.Filters;
using Microsoft.Extensions.Logging;

namespace Eventuous.Projections.SqlServer
{
    public class
        TransactionalAllStreamSubscriptionService :
            EventSubscriptionWithCheckpoint<SubscriptionOptions>
    {
        private readonly ILogger _log;
        private readonly SubscriptionOptions _options;
        private readonly EventStoreClient _eventStoreClient;

        public TransactionalAllStreamSubscriptionService(
            SubscriptionOptions options,
            ICheckpointStore checkpointStore,
            ConsumePipe consumePipe,
            int concurrencyLimit,
            ILoggerFactory? loggerFactory,
            EventStoreClient eventStoreClient) : base(options, checkpointStore, consumePipe, concurrencyLimit, loggerFactory)
        {
            _eventStoreClient = eventStoreClient;
        }

        protected override async ValueTask Subscribe(CancellationToken cancellationToken)
        {
            var filterOptions = new SubscriptionFilterOptions(
                EventTypeFilter.ExcludeSystemEvents(),
                10,
                (_, p, ct) =>
                    StoreCheckpoint(
                        new EventPosition(p.CommitPosition, DateTime.UtcNow),
                        ct));

            var (_, position) = await GetCheckpoint(cancellationToken).NoContext();
            var subscribeTask = position != null
                ? _eventStoreClient.SubscribeToAllAsync(
                    FromAll.After(new Position(position.Value, position.Value)),
                    TransactionalHandler,
                    false,
                    HandleDrop,
                    filterOptions,
                    cancellationToken: cancellationToken)
                : _eventStoreClient.SubscribeToAllAsync(
                    FromAll.Start,
                    TransactionalHandler,
                    false,
                    HandleDrop,
                    filterOptions,
                    cancellationToken: cancellationToken);

            var sub = await subscribeTask.NoContext();
        }

        protected override ValueTask Unsubscribe(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void HandleDrop(
            StreamSubscription arg1,
            SubscriptionDroppedReason arg2,
            Exception? arg3)
        {
            _log.LogWarning($"Subscription {SubscriptionId} dropped. Reason: {arg2}");
        }

        /// <summary>
        /// Wrap event handling and checkpoint updates in a transaction.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <param name="ct"></param>
        private async Task TransactionalHandler(
            StreamSubscription _,
            ResolvedEvent e,
            CancellationToken ct)
        {
            try
            {
                _log.LogDebug($"Subscription {SubscriptionId} got an event {e.Event.EventType}");
                using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                await Handler(AsReceivedEvent(e), ct);

                tx.Complete();
            }
            catch (Exception exception)
            {
                _log.LogError(exception.ToString());
                throw;
            }

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
                );
            }
        }
    }
}