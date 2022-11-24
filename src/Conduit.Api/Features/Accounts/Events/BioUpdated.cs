using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("BioUpdated")]
    public record BioUpdated(string StreamId, string Bio);
}