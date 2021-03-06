using System;
using Conduit.Api.Fakes;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Accounts.Events;
using Eventuous;

namespace Conduit.Api.Tests
{
    public static class Fixtures
    {
        public static UserService UserService
        {
            get
            {
                AccountsRegistration.Register();
                var aggregateStore = new AggregateStore(
                    new InMemoryEventStore(),
                    DefaultEventSerializer.Instance);
                return new UserService(aggregateStore);
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