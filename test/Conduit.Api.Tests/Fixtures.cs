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

        private static UserRegistration? _instance;

        public static void SetupAccountFixture(string email, string username, string password) => _instance =
            new UserRegistration(Guid.NewGuid().ToString("N"), email, username, password);

        public static UserRegistration UserRegistration =>
            _instance ??= new UserRegistration(Guid.NewGuid().ToString("N"),
                "jake@jake.jake",
                "jake",
                "jakejake");
    }
}