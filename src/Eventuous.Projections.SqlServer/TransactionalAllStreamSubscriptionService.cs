using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventStore.Client;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.EventStoreDB;
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
            _log.LogWarning($"Subscription {SubscriptionId} dropped. Reason: {arg2}");
            
        }

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