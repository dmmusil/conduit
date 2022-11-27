using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Accounts.Commands;
using Conduit.Api.Features.Articles;
using Conduit.Api.Features.Articles.Commands;
using Conduit.Api.Features.Articles.Events;
using Conduit.Api.Features.Articles.Projectors;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Conduit.Api.Tests.Integration
{
    public class Accounts : IClassFixture<WebApplicationFactory<Startup>>
    {
        private static readonly string UniqueEmail = $"{Uid}@email.com";
        private static readonly string UniqueUsername = Uid;

        private readonly WebApplicationFactory<Startup> _factory;
        private static string Uid => Guid.NewGuid().ToString("N");

        public Accounts(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            Fixtures.SetupAccountFixture($"{Uid}@jake.jake", Uid, Uid);
        }

        [Fact]
        public async Task Can_register_login_and_retrieve_current_user()
        {
            var client = _factory.CreateClient();
            var config = _factory.Services.GetService<IConfiguration>();

            var accountId = await Register(client);
            var token = await Login(client, accountId);
            await Verify(client, token);
            await Update(client, token);
            await AttemptRegisterDuplicate(client);
            var uniqueId = await RegisterUniqueUser(client);
            await AttemptUpdateDuplicate(client, token!);

            await GetProfile(client);

            await Follow(client, UniqueUsername);
            await GenerateFeed(uniqueId, config!);
            await LoadFeed(client, 1);
            await Unfollow(client, UniqueUsername);
            await LoadFeed(client, 0);

            await PublishArticle(client);
            await FavoriteArticle(client);
            await UnfavoriteArticle(client);
            await UpdateArticle(client);
            await GetTags(client, 3);
            await DeleteArticle(client);
            await GetTags(client, 0);
        }

        private async Task LoadFeed(HttpClient client, int expectedCount)
        {
            var response = await client.GetAsync("/api/articles/feed");
            var feed = await response.Content.ReadFromJsonAsync<FeedEnvelope>();
            Assert.Equal(expectedCount, feed!.Articles.Count());
        }

        private async Task GenerateFeed(string uniqueId, IConfiguration configuration)
        {
            var title = Uid;
            // add an article by the followed user to appear in the test user's feed
            var articleInsert = new ArticleInsertCommand(
                new ArticlePublished(
                    Guid.NewGuid().ToString("N"),
                    title,
                    title.ToSlug(),
                    "Follow me",
                    "Follow me",
                    uniqueId,
                    UniqueUsername,
                    null,
                    null,
                    DateTime.UtcNow,
                    new[] { "doesn't matter" }
                )
            );

            await using var connection = new SqlConnection(
                configuration.GetConnectionString("ReadModels")
            );

            await connection.ExecuteAsync(articleInsert.Command);
        }

        private async Task GetTags(HttpClient client, int expectedCount)
        {
            var response = await client.GetAsync("/api/tags");
            var tags = await response.Content.ReadFromJsonAsync<TagsEnvelope>();
            Assert.Equal(expectedCount, tags!.Tags.Count());
        }

        private static async Task FavoriteArticle(HttpClient client) =>
            await ManageFavorites(client, HttpMethod.Post);

        private static async Task ManageFavorites(HttpClient client, HttpMethod method)
        {
            const string slug = "how-to-train-your-dragon";

            if (method == HttpMethod.Post)
                await client.PostAsync($"/api/articles/{slug}/favorite", new StringContent(""));
            else
                await client.DeleteAsync($"/api/articles/{slug}/favorite");

            var expectedFavoriteCount = method == HttpMethod.Post ? 1 : 0;

            await GetFromProjection<ArticleEnvelope>(
                client,
                $"/api/articles/{slug}",
                HttpStatusCode.OK,
                article => article.Article.FavoritesCount == expectedFavoriteCount
            );
        }

        private static async Task UnfavoriteArticle(HttpClient client) =>
            await ManageFavorites(client, HttpMethod.Delete);

        private static async Task DeleteArticle(HttpClient client)
        {
            const string slug = "how-not-to-train-your-dragon";

            await GetFromProjection(client, $"/api/articles/{slug}", HttpStatusCode.OK);

            var response = await client.DeleteAsync($"/api/articles/{slug}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await GetFromProjection(client, $"/api/articles/{slug}", HttpStatusCode.NotFound);
        }

        private static async Task UpdateArticle(HttpClient client)
        {
            const string slug = "how-to-train-your-dragon";
            var response = await SendCommand(
                client,
                new UpdateEnvelope(
                    new UpdateArticle(
                        null,
                        "How not to train your dragon",
                        "Lessons learned",
                        "Don't play with fire."
                    )
                ),
                $"/api/articles/{slug}",
                method: HttpMethod.Put
            );
            var envelope = await response.Content.ReadFromJsonAsync<ArticleEnvelope>();
            Assert.Equal("Don't play with fire.", envelope?.Article.Body);
            Assert.NotNull(envelope!.Article.UpdatedAt);
        }

        private static async Task PublishArticle(HttpClient client)
        {
            var response = await SendCommand(
                client,
                new CreateEnvelope(
                    new CreateArticle(
                        "How to train your dragon",
                        "Ever wonder how?",
                        "You have to believe",
                        new[] { "reactjs", "angularjs", "dragons" }
                    )
                ),
                "/api/articles"
            );

            var body = await response.Content.ReadFromJsonAsync<ArticleEnvelope>();

            const string expectedSlug = "how-to-train-your-dragon";
            Assert.Equal(expectedSlug, body?.Article.Slug);

            response = await GetFromProjection(
                client,
                $"/api/articles/{expectedSlug}",
                HttpStatusCode.OK
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            body = await response.Content.ReadFromJsonAsync<ArticleEnvelope>();

            Assert.Equal(Fixtures.UserRegistration.Username, body?.Article.Author.Username);
        }

        private static async Task GetFromProjection<T>(
            HttpClient client,
            string route,
            HttpStatusCode expectedStatusCode,
            Func<T, bool> test
        )
        {
            var stopwatch = Stopwatch.StartNew();

            var t = default(T);
            HttpResponseMessage response = null!;
            while (stopwatch.ElapsedMilliseconds < 2000)
            {
                response = await client.GetAsync(route);

                if (response.StatusCode != expectedStatusCode)
                    continue;
                t = await response.Content.ReadFromJsonAsync<T>();
                if (test(t!))
                {
                    return;
                }
            }

            throw new Exception(
                $"Projection didn't run within 2 seconds. Response code: {response.StatusCode} Body: {t}"
            );
        }

        private static async Task<HttpResponseMessage> GetFromProjection(
            HttpClient client,
            string route,
            HttpStatusCode expectedStatusCode
        )
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < 2000)
            {
                var response = await client.GetAsync(route);

                if (response.StatusCode == expectedStatusCode)
                    return response;
            }

            throw new Exception("Projection didn't run within 2 seconds.");
        }

        private static async Task Follow(HttpClient client, string usernameToFollow)
        {
            var response = await client.PostAsync(
                $"/api/profiles/{usernameToFollow}/follow",
                new StringContent("")
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await GetFromProjection<ProfileEnvelope>(
                client,
                $"/api/profiles/{usernameToFollow}",
                HttpStatusCode.OK,
                profile => profile.Profile.Following
            );
        }

        private static async Task Unfollow(HttpClient client, string usernameToUnfollow)
        {
            var response = await client.DeleteAsync($"/api/profiles/{usernameToUnfollow}/follow");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await GetFromProjection<ProfileEnvelope>(
                client,
                $"/api/profiles/{usernameToUnfollow}",
                HttpStatusCode.OK,
                profile => profile.Profile.Following == false
            );
        }

        private static async Task GetProfile(HttpClient client)
        {
            var response = await GetFromProjection(
                client,
                $"/api/profiles/{Fixtures.UserRegistration.Username}",
                HttpStatusCode.OK
            );
            var profile = await response.Content.ReadFromJsonAsync<ProfileEnvelope>();

            Assert.Equal(Fixtures.UserRegistration.Username, profile?.Profile.Username);
        }

        private static async Task AttemptUpdateDuplicate(HttpClient client, string token)
        {
            var command = new UpdateUser(null, new UserUpdate(Username: UniqueUsername));
            await SendCommand(
                client,
                command,
                "/api/user",
                method: HttpMethod.Put,
                headers: new Dictionary<string, string> { { "Authorization", $"Bearer {token}" } },
                expectedResponseCode: HttpStatusCode.Conflict
            );

            command = new UpdateUser(null, new UserUpdate(Email: UniqueEmail));
            await SendCommand(
                client,
                command,
                "/api/user",
                method: HttpMethod.Put,
                headers: new Dictionary<string, string> { { "Authorization", $"Bearer {token}" } },
                expectedResponseCode: HttpStatusCode.Conflict
            );
        }

        private static async Task<string> RegisterUniqueUser(HttpClient client)
        {
            var command = new Register(
                Fixtures.UserRegistration with
                {
                    Id = Uid,
                    Email = UniqueEmail,
                    Username = UniqueUsername
                }
            );
            var response = await SendCommand(client, command, "/api/users");
            var envelope = await response.Content.ReadFromJsonAsync<UserEnvelope>();

            return envelope!.User.Id;
        }

        private static async Task AttemptRegisterDuplicate(HttpClient client)
        {
            var command = new Register(Fixtures.UserRegistration);
            await SendCommand(client, command, "/api/users", HttpStatusCode.Conflict);
        }

        private static async Task Update(HttpClient client, string? token)
        {
            var command = new UpdateUser(null, new UserUpdate(Bio: "I work at State Farm."));
            await SendCommand(
                client,
                command,
                "/api/user",
                method: HttpMethod.Put,
                headers: new Dictionary<string, string> { { "Authorization", $"Bearer {token}" } }
            );
        }

        private static async Task<string> Register(HttpClient client)
        {
            var command = new Register(Fixtures.UserRegistration);
            var response = await SendCommand(client, command, "/api/users");
            var user = await response.Content.ReadFromJsonAsync<UserEnvelope>();

            await GetFromProjection(
                client,
                $"/api/profiles/{Fixtures.UserRegistration.Username}",
                HttpStatusCode.OK
            );

            return user!.User.Id;
        }

        private static async Task<string?> Login(HttpClient client, string id)
        {
            var command = new LogIn(
                id,
                new UserLogin(Fixtures.UserRegistration.Email, Fixtures.UserRegistration.Password)
            );
            var response = await SendCommand(client, command, "/api/users/login");
            var user = await response.Content.ReadFromJsonAsync<UserEnvelope>();
            return user?.User.Token;
        }

        private static async Task Verify(HttpClient client, string? token)
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var response = await client.GetAsync("/api/user");
            var user = await response.Content.ReadFromJsonAsync<UserEnvelope>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Fixtures.UserRegistration.Username, user?.User.Username);
        }

        private static async Task<HttpResponseMessage> SendCommand(
            HttpClient client,
            object command,
            string route,
            HttpStatusCode expectedResponseCode = HttpStatusCode.OK,
            HttpMethod? method = null,
            Dictionary<string, string>? headers = null
        )
        {
            if (method == null)
                method = HttpMethod.Post;
            var request = new HttpRequestMessage(method, route)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(command),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            if (headers != null)
                foreach (var (key, value) in headers)
                    request.Headers.Add(key, value);

            var response = await client.SendAsync(request);

            Assert.Equal(expectedResponseCode, response.StatusCode);

            return response;
        }
    }
}
