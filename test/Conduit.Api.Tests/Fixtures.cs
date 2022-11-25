using System;
using Conduit.Api.Fakes;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Accounts.Events;
using Eventuous;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Conduit.Api.Tests
{
    public static class Fixtures
    {
        public static UserService UserService
        {
            get
            {
                var aggregateStore = new AggregateStore(new InMemoryEventStore());
                return new UserService(aggregateStore, new StreamNameMap(), new NoOpCheckpointStore(),
                    new NullLoggerFactory());
            }
        }

        public static UserRegistration UserRegistration =>
            new(Guid.NewGuid().ToString("N"),
                "jake@jake.jake",
                "jake",
                "jakejake");

        public static UserLogin UserLogin => new("jake@jake.jake", "jakejake");
    }
}