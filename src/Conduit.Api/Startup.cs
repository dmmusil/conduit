using Conduit.Api.Auth;
using Conduit.Api.Features;
using EventStore.Client;
using Eventuous;
using Eventuous.EventStoreDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conduit.Api
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services
                .AddSingleton(_ =>
                    new EventStoreClient(EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false")))
                .AddSingleton<IEventStore, EsDbEventStore>()
                .AddSingleton(DefaultEventSerializer.Instance)
                .AddSingleton<AppSettings>()
                .AddScoped<IAggregateStore, AggregateStore>()
                .AddScoped<Accounts.UserService>()
                .AddScoped<JwtIssuer>()
                .AddCors()
                .AddControllers();

            Accounts.Events.Register();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(x => x.MapControllers());
        }
    }
}