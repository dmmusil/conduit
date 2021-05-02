using System;
using System.Collections.Immutable;
using Eventuous;

namespace Conduit.Api.Features.Accounts.Aggregates
{
    public class Account : Aggregate<AccountState, AccountId>
    {
        public void Register(string username, string email, string passwordHash)
        {
            if (State.AlreadyRegistered) return;
            Apply(
                new Events.UserRegistered(
                    Guid.NewGuid().ToString("N"),
                    email,
                    username,
                    passwordHash));
        }

        public void Update(
            string? email,
            string? username,
            string? password,
            string? bio,
            string? image)
        {
            EnsureExists();
            if (email != null)
                Apply(new Events.EmailUpdated(State.Id, email));
            if (username != null)
                Apply(new Events.UsernameUpdated(State.Id, username));
            if (password != null)
                Apply(
                    new Events.PasswordUpdated(
                        State.Id,
                        BCrypt.Net.BCrypt.HashPassword(password)));
            if (bio != null)
                Apply(new Events.BioUpdated(State.Id, bio));
            if (image != null)
                Apply(new Events.ImageUpdated(State.Id, image));
        }

        public void Follow(string followedId)
        {
            EnsureExists();
            if (State.AlreadyFollowing(followedId)) return;
            Apply(new Events.AccountFollowed(State.Id, followedId));
        }

        public void Unfollow(string followedId)
        {
            EnsureExists();
            if (State.NotFollowing(followedId)) return;
            Apply(new Events.AccountUnfollowed(State.Id, followedId));
        }
    }

    public record AccountId(string Value) : AggregateId(Value);

    public record AccountState : AggregateState<AccountState, AccountId>
    {
        public override AccountState When(object @event)
        {
            return @event switch
            {
                Events.UserRegistered(var streamId, var email, var username, var
                    passwordHash) => this with
                    {
                        Id = new AccountId(streamId),
                        Email = email,
                        Username = username,
                        PasswordHash = passwordHash
                    },
                Events.EmailUpdated(_, var email) => this with {Email = email},
                Events.UsernameUpdated(_, var username) => this with
                {
                    Username = username
                },
                Events.BioUpdated(_, var bio) => this with {Bio = bio},
                Events.PasswordUpdated(_, var passwordHash) => this with
                {
                    PasswordHash = passwordHash
                },
                Events.ImageUpdated(_, var image) => this with {Image = image},
                Events.AccountFollowed e => this with
                {
                    FollowedProfiles = FollowedProfiles.Add(e.FollowedId)
                },
                Events.AccountUnfollowed e => this with
                {
                    FollowedProfiles = FollowedProfiles.Remove(e.UnfollowedId)
                },
                _ => throw new ArgumentOutOfRangeException(
                    nameof(@event),
                    "Unknown event")
            };
        }

        public string PasswordHash { get; private init; } = null!;
        public string Email { get; private init; } = null!;
        public string Username { get; private init; } = null!;
        public string? Bio { get; private init; }
        public string? Image { get; private init; }

        private ImmutableHashSet<string> FollowedProfiles { get; init; } =
            ImmutableHashSet<string>.Empty;

        public bool AlreadyRegistered => Id != null;

        public bool AlreadyFollowing(string followedId) =>
            FollowedProfiles.Contains(followedId);

        public bool NotFollowing(string followedId) =>
            !AlreadyFollowing(followedId);
    }
}