using System.Collections.Generic;
using EventStore.Client;
using Eventuous;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.EventStoreDB;
using Microsoft.Extensions.Logging;

namespace Conduit.Api.Infrastructure
{
    public class ConduitSubscriber : AllStreamSubscription
    {
        public ConduitSubscriber(EventStoreClient eventStoreClient,
            string subscriptionId, ICheckpointStore checkpointStore,
            IEnumerable<IEventHandler> eventHandlers,
            IEventSerializer? eventSerializer = null,
            ILoggerFactory? loggerFactory = null,
            IEventFilter? eventFilter = null,
            ISubscriptionGapMeasure? measure = null) : base(eventStoreClient,
            subscriptionId, checkpointStore, eventHandlers, eventSerializer,
            loggerFactory, eventFilter, measure)
        {
        }

        public ConduitSubscriber(EventStoreClient eventStoreClient,
            AllStreamSubscriptionOptions options,
            ICheckpointStore checkpointStore,
            IEnumerable<IEventHandler> eventHandlers,
            IEventSerializer? eventSerializer = null,
            ILoggerFactory? loggerFactory = null,
            IEventFilter? eventFilter = null,
            ISubscriptionGapMeasure? measure = null) : base(eventStoreClient,
            options, checkpointStore, eventHandlers, eventSerializer,
            loggerFactory, eventFilter, measure)
        {
        }
    }
}