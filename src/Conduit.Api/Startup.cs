using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Accounts.Events;
using Conduit.Api.Features.Accounts.Projectors;
using Conduit.Api.Features.Accounts.Queries;
using Conduit.Api.Features.Articles;
using Conduit.Api.Features.Articles.Events;
using Conduit.Api.Features.Articles.Projectors;
using Conduit.Api.Features.Articles.Queries;
using Conduit.Api.Infrastructure;
using EventStore.Client;
using Eventuous;
using Eventuous.EventStoreDB;
using Eventuous.Projections.SqlServer;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.EventStoreDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
                .AddSingleton(
                    _ => new EventStoreClient(
                        EventStoreClientSettings.Create(
                            "esdb://admin:changeit@localhost:2113?tls=false")))
                .AddSingleton<IEventStore, EsdbEventStore>()
                .AddSingleton(DefaultEventSerializer.Instance)
                .AddSingleton<AppSettings>()
                .AddScoped<IAggregateStore, AggregateStore>()
                .AddScoped<UserService>()
                .AddScoped<UserRepository>()
                .AddScoped<ArticleRepository>()
                .AddScoped<ArticleService>()
                .AddScoped<JwtIssuer>()
                .AddCors()
                .AddControllers();

            AccountsRegistration.Register();
            ArticlesRegistration.Register();

            const string connectionString =
                "mongodb://mongoadmin:secret@localhost:27017/?authSource=admin&readPreference=primary&ssl=false";
            services.AddSingleton(_ => new MongoClient(connectionString));
            services.AddSingleton(
                    o => o.GetService<MongoClient>()!.GetDatabase("Conduit"))
                .AddSingleton<ICheckpointStore, SqlServerCheckpointStore>();

            services.AddSingleton(
                o => new ConduitSubscriber(
                    o.GetService<EventStoreClient>()!,
                    "Conduit",
                    o.GetService<ICheckpointStore>()!,
                    new IEventHandler[]
                    {
                        new AccountsEventHandler(
                            o.GetService<IMongoDatabase>()!,
                            "Conduit",
                            o.GetService<ILoggerFactory>()!),
                        new ArticleEventHandler(
                            o.GetService<IMongoDatabase>()!,
                            "Conduit",
                            o.GetService<ILoggerFactory>()!),
                        new TagsEventHandler(
                            o.GetService<IMongoDatabase>()!,
                            "Conduit",
                            o.GetService<ILoggerFactory>()!),
                    },
                    o.GetService<IEventSerializer>()!,
                    o.GetService<ILoggerFactory>()!));
            services.AddSingleton(o =>
                new TransactionalAllStreamSubscriptionService(
                    o.GetService<EventStoreClient>(),
                    new AllStreamSubscriptionOptions
                        { SubscriptionId = "ConduitSql" },
                    o.GetService<ICheckpointStore>(), new IEventHandler[]
                    {
                        new SqlAccountsEventHandler(
                            o.GetService<IConfiguration>()!, "ConduitSql",
                            o.GetService<ILoggerFactory>()!)

                    }, o.GetService<IEventSerializer>()!,
                    o.GetService<ILoggerFactory>()));
            
            services.AddHostedService(o => o.GetService<ConduitSubscriber>());
            services.AddHostedService(o => o.GetService<TransactionalAllStreamSubscriptionService>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors(
                x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(x => x.MapControllers());
        }
    }
}