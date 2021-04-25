using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Conduit.Api.Features.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Conduit.Api.Tests.Integration
{
    public class Accounts : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string UniqueEmail = "unique@email.com";
        private const string UniqueUsername = "unique";

        private readonly WebApplicationFactory<Startup> _factory;

        public Accounts(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Can_register_login_and_retrieve_current_user()
        {
            var client = _factory.CreateClient();

            var accountId = await Register(client);
            var token = await Login(client, accountId);
            await Verify(client, token);
            await Update(client, token);
            await AttemptRegisterDuplicate(client);
            await RegisterUniqueUser(client);
            await AttemptUpdateDuplicate(client, token!);

            await GetProfile(client);
        }

        private static async Task GetProfile(HttpClient client)
        {
            var response = await client.GetAsync($"/api/profiles/{Fixtures.UserRegistration.Username}");
            var profile = await response.Content.ReadFromJsonAsync<Profile>();
            
            Assert.Equal(Fixtures.UserRegistration.Username, profile?.Username);
        }

        private static async Task AttemptUpdateDuplicate(HttpClient client, string token)
        {
            var command = new Features.Accounts.Commands.UpdateUser(null,
                new Features.Accounts.UserUpdate(Username: UniqueUsername));
            await SendCommand(client,
                command, "/api/user",
                method: HttpMethod.Put, headers: new Dictionary<string, string>
                {
                    {"Authorization", $"Bearer {token}"}
                },
                expectedResponseCode: HttpStatusCode.Conflict);

            command = new Features.Accounts.Commands.UpdateUser(null,
                new Features.Accounts.UserUpdate(Email: UniqueEmail));
            await SendCommand(client,
                command, "/api/user",
                method: HttpMethod.Put, headers: new Dictionary<string, string>
                {
                    {"Authorization", $"Bearer {token}"}
                },
                expectedResponseCode: HttpStatusCode.Conflict);
        }

        private static async Task RegisterUniqueUser(HttpClient client)
        {
            var command = new Features.Accounts.Commands.Register(Fixtures.UserRegistration with
            {
                Email = UniqueEmail,
                Username = UniqueUsername
            });
            var response = await SendCommand(client, command, "/api/users/register");
            await response.Content.ReadFromJsonAsync<Features.Accounts.User>();
        }

        private static async Task AttemptRegisterDuplicate(HttpClient client)
        {
            var command = new Features.Accounts.Commands.Register(Fixtures.UserRegistration);
            await SendCommand(client, command, "/api/users/register", HttpStatusCode.Conflict);
        }

        private static async Task Update(HttpClient client, string? token)
        {
            var command = new Features.Accounts.Commands.UpdateUser(null,
                new Features.Accounts.UserUpdate(Bio: "I work at State Farm."));
            await SendCommand(client,
                command, "/api/user",
                method: HttpMethod.Put, headers: new Dictionary<string, string>
                {
                    {"Authorization", $"Bearer {token}"}
                });
        }

        private static async Task<string> Register(HttpClient client)
        {
            var command = new Features.Accounts.Commands.Register(Fixtures.UserRegistration);
            var response = await SendCommand(client, command, "/api/users/register");
            var user = await response.Content.ReadFromJsonAsync<Features.Accounts.User>();
            return user!.Id;
        }

        private static async Task<string?> Login(HttpClient client, string id)
        {
            var command = new Features.Accounts.Commands.LogIn(id, Fixtures.UserLogin);
            var response = await SendCommand(client, command, "/api/users/login");
            var user = await response.Content.ReadFromJsonAsync<Features.Accounts.User>();
            return user?.Token;
        }

        private static async Task Verify(HttpClient client, string? token)
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var response = await client.GetAsync("/api/user");
            var user = await response.Content.ReadFromJsonAsync<Features.Accounts.User>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("jake", user?.Username);
        }

        private static async Task<HttpResponseMessage> SendCommand(
            HttpClient client,
            object command,
            string route,
            HttpStatusCode expectedResponseCode = HttpStatusCode.OK,
            HttpMethod? method = null,
            Dictionary<string, string>? headers = null)

        {
            if (method == null) method = HttpMethod.Post;
            var request = new HttpRequestMessage(method, route)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(command),
                    Encoding.UTF8,
                    "application/json")
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