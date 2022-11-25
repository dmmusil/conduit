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
        private readonly EventStoreClient _eventStoreClient;
        private StreamSubscription _sub;

        public TransactionalAllStreamSubscriptionService(
            AllStreamSubscriptionOptions options,
            ICheckpointStore checkpointStore,
            ConsumePipe consumePipe,
            ILoggerFactory loggerFactory,
            EventStoreClient eventStoreClient) : base(options, checkpointStore, consumePipe, 1, loggerFactory)
        {
            _eventStoreClient = eventStoreClient;
            _log = loggerFactory.CreateLogger<TransactionalAllStreamSubscriptionService>();
        }

        protected override async ValueTask Subscribe(CancellationToken cancellationToken)
        {
            var filterOptions = new SubscriptionFilterOptions(
                EventTypeFilter.ExcludeSystemEvents(),
                1,
                (_, p, ct) =>
                    StoreCheckpoint(
                        new EventPosition(p.PreparePosition, DateTime.UtcNow),
                        ct));

            var (_, position) = await GetCheckpoint(cancellationToken).NoContext();

            var from = position != null
                ? FromAll.After(new Position(position.Value, position.Value))
                : FromAll.Start;

            var subscribeTask = _eventStoreClient.SubscribeToAllAsync(
                from,
                TransactionalHandler,
                false,
                HandleDrop,
                filterOptions,
                cancellationToken: cancellationToken);

            _sub = await subscribeTask.NoContext();
        }

        protected override ValueTask Unsubscribe(CancellationToken cancellationToken)
        {
            _sub.Dispose();
            return ValueTask.CompletedTask;
        }

        private void HandleDrop(
            StreamSubscription arg1,
            SubscriptionDroppedReason arg2,
            Exception? arg3)
        {
            _log.LogWarning($"Subscription {SubscriptionId} dropped. Reason: {arg2}");
        }

        private ulong _sequence;

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
                var result =
                    DefaultEventSerializer.Instance.DeserializeEvent(evt.Data.Span, evt.EventType, "application/json");
                object? message = null;
                if (result is SuccessfullyDeserialized s)
                {
                    message = s.Payload;
                }
                _log.LogDebug($"Subscription {SubscriptionId} got an event {evt.EventType}");
                var context = new MessageConsumeContext(
                    evt.EventId.ToString(),
                    evt.EventType,
                    "application/json",
                    evt.EventStreamId,
                    evt.EventNumber,
                    evt.Position.CommitPosition,
                    evt.Position.CommitPosition,
                    evt.Created,
                    message,
                    Options.MetadataSerializer.DeserializeMeta(Options, evt.Metadata, e.OriginalStreamId),
                    SubscriptionId,
                    ct
                );

                await HandleInternal(context);
            }
            catch (Exception exception)
            {
                _log.LogError(exception.ToString());
                throw;
            }
        }
    }
}