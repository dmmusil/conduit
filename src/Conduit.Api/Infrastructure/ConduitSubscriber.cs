using System.Collections.Generic;
using EventStore.Client;
using Eventuous;
using Eventuous.EventStoreDB.Subscriptions;
using Microsoft.Extensions.Logging;

namespace Conduit.Api.Infrastructure
{
    public class ConduitSubscriber : AllStreamSubscriptionService
    {
        public ConduitSubscriber(EventStoreClient eventStoreClient, string subscriptionId,
            ICheckpointStore checkpointStore, IEventSerializer eventSerializer,
            IEnumerable<IEventHandler> eventHandlers, ILoggerFactory loggerFactory = null!,
            IEventFilter eventFilter = null!, SubscriptionGapMeasure measure = null!) : base(eventStoreClient,
            subscriptionId, checkpointStore, eventSerializer, eventHandlers, loggerFactory, eventFilter, measure)
        {
        }
    }
}