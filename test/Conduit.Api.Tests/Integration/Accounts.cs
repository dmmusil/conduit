using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Conduit.Api.Tests.Integration
{
    public class Accounts : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public Accounts(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Can_register_login_and_retrieve_current_user()
        {
            var client = _factory.CreateClient();

            await Register(client);
            var token = await Login(client);
            await Verify(client, token);
        }
        
        private static async Task Register(HttpClient client)
        {
            var command = new Features.Accounts.Commands.Register(Fixtures.UserRegistration);
            await PostCommand(client, command, "/api/users/register");
        }
        
        private static async Task<string?> Login(HttpClient client)
        {
            var command = new Features.Accounts.Commands.LogIn(Fixtures.UserLogin);
            var response = await PostCommand(client, command, "/api/users/login");
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

        private static async Task<HttpResponseMessage> PostCommand(
            HttpClient client, 
            object command, 
            string route,
            HttpStatusCode expectedResponseCode = HttpStatusCode.OK)
        {
            var response = await client.PostAsync(
                route,
                new StringContent(
                    JsonSerializer.Serialize(command),
                    Encoding.UTF8,
                    "application/json"));

            Assert.Equal(expectedResponseCode, response.StatusCode);

            return response;
        }
    }
}