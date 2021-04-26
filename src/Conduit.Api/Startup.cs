using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Infrastructure;
using EventStore.Client;
using Eventuous;
using Eventuous.EventStoreDB;
using Eventuous.EventStoreDB.Subscriptions;
using Eventuous.Projections.MongoDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

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
                    new EventStoreClient(
                        EventStoreClientSettings.Create("esdb://admin:changeit@localhost:2113?tls=false")))
                .AddSingleton<IEventStore, EsDbEventStore>()
                .AddSingleton(DefaultEventSerializer.Instance)
                .AddSingleton<AppSettings>()
                .AddScoped<IAggregateStore, AggregateStore>()
                .AddScoped<UserService>()
                .AddScoped<UserRepository>()
                .AddScoped<JwtIssuer>()
                .AddCors()
                .AddControllers();

            Events.Register();
            
            const string connectionString = "mongodb://mongoadmin:secret@localhost:27017/?authSource=admin&readPreference=primary&ssl=false";
            services.AddSingleton(_ => new MongoClient(connectionString));
            services.AddSingleton(o => o.GetService<MongoClient>()!.GetDatabase("Conduit"))
                .AddSingleton<ICheckpointStore, MongoCheckpointStore>();
            
            services.AddSingleton(o => new ConduitSubscriber(
                o.GetService<EventStoreClient>()!,
                "Conduit",
                o.GetService<ICheckpointStore>()!,
                o.GetService<IEventSerializer>()!,
                new IEventHandler[]
                {
                    new AccountsEventHandler(
                        o.GetService<IMongoDatabase>()!, 
                        "Conduit", 
                        o.GetService<ILoggerFactory>()!)
                },
                o.GetService<ILoggerFactory>()!
            ));
            services.AddHostedService(o => o.GetService<ConduitSubscriber>());
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