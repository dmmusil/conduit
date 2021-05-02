using System;
using System.Collections.Immutable;
using Conduit.Api.Features.Accounts.Events;
using Eventuous;

namespace Conduit.Api.Features.Accounts.Aggregates
{
    public class Account : Aggregate<AccountState, AccountId>
    {
        public void Register(string username, string email, string passwordHash)
        {
            if (State.AlreadyRegistered) return;
            Apply(
                new UserRegistered(
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
                Apply(new EmailUpdated(State.Id, email));
            if (username != null)
                Apply(new UsernameUpdated(State.Id, username));
            if (password != null)
                Apply(
                    new PasswordUpdated(
                        State.Id,
                        BCrypt.Net.BCrypt.HashPassword(password)));
            if (bio != null)
                Apply(new BioUpdated(State.Id, bio));
            if (image != null)
                Apply(new ImageUpdated(State.Id, image));
        }

        public void Follow(string followedId)
        {
            EnsureExists();
            if (State.AlreadyFollowing(followedId)) return;
            Apply(new AccountFollowed(State.Id, followedId));
        }

        public void Unfollow(string followedId)
        {
            EnsureExists();
            if (State.NotFollowing(followedId)) return;
            Apply(new AccountUnfollowed(State.Id, followedId));
        }
    }

    public record AccountId(string Value) : AggregateId(Value);

    public record AccountState : AggregateState<AccountState, AccountId>
    {
        public override AccountState When(object @event)
        {
            return @event switch
            {
                UserRegistered(var streamId, var email, var username, var
                    passwordHash) => this with
                    {
                        Id = new AccountId(streamId),
                        Email = email,
                        Username = username,
                        PasswordHash = passwordHash
                    },
                EmailUpdated(_, var email) => this with {Email = email},
                UsernameUpdated(_, var username) => this with
                {
                    Username = username
                },
                BioUpdated(_, var bio) => this with {Bio = bio},
                PasswordUpdated(_, var passwordHash) => this with
                {
                    PasswordHash = passwordHash
                },
                ImageUpdated(_, var image) => this with {Image = image},
                AccountFollowed e => this with
                {
                    FollowedProfiles = FollowedProfiles.Add(e.FollowedId)
                },
                AccountUnfollowed e => this with
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