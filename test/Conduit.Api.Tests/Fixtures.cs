using Conduit.Api.Fakes;
using Conduit.Api.Features;
using Eventuous;

namespace Conduit.Api.Tests
{
    public static class Fixtures
    {
        public static Features.Accounts.UserService UserService
        {
            get
            {
                Features.Accounts.Events.Register();
                var aggregateStore = new AggregateStore(
                    new InMemoryEventStore(),
                    DefaultEventSerializer.Instance);
                return new Features.Accounts.UserService(aggregateStore);
            }
        }

        public static Features.Accounts.UserRegistration UserRegistration =>
            new("jake@jake.jake", "jake", "jakejake");

        public static Accounts.UserLogin UserLogin => new("jake@jake.jake", "jakejake");
    }
}