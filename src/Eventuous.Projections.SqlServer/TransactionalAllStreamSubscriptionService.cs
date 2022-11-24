using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventStore.Client;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Checkpoints;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Microsoft.Extensions.Logging;
using StreamSubscription = EventStore.Client.StreamSubscription;

namespace Eventuous.Projections.SqlServer
{
    public class
        TransactionalAllStreamSubscriptionService :
            EventSubscriptionWithCheckpoint<AllStreamSubscriptionOptions>
    {
        private readonly ILogger _log;
        private readonly SubscriptionOptions _options;
        private readonly EventStoreClient _eventStoreClient;

        public TransactionalAllStreamSubscriptionService(
            AllStreamSubscriptionOptions options,
            ICheckpointStore checkpointStore,
            ConsumePipe consumePipe,
            int concurrencyLimit,
            ILoggerFactory? loggerFactory,
            EventStoreClient eventStoreClient) : base(options, checkpointStore, consumePipe, 1, loggerFactory)
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
                var evt = e.Event;
                _log.LogDebug($"Subscription {SubscriptionId} got an event {evt.EventType}");
                using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                var context = new MessageConsumeContext(
                    evt.EventId.ToString(),
                    evt.EventType,
                    "application/json",
                    evt.EventStreamId,
                    evt.EventNumber,
                    evt.Position.CommitPosition,
                    evt.Position.CommitPosition,
                    evt.Created,
                    e,
                    Options.MetadataSerializer.DeserializeMeta(Options, evt.Metadata, e.OriginalStreamId),
                    SubscriptionId,
                    ct
                );
                await Handler(context);

                tx.Complete();
            }
            catch (Exception exception)
            {
                _log.LogError(exception.ToString());
                throw;
            }
        }
    }
}