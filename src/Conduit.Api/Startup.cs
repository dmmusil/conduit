using System.Data;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Accounts.Aggregates;
using Conduit.Api.Features.Accounts.Projectors;
using Conduit.Api.Features.Accounts.Queries;
using Conduit.Api.Features.Articles;
using Conduit.Api.Features.Articles.Aggregates;
using Conduit.Api.Features.Articles.Projectors;
using Conduit.Api.Features.Articles.Queries;
using Conduit.ReadModels;
using EventStore.Client;
using Eventuous;
using Eventuous.EventStore;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Projections.SqlServer;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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
                .AddSingleton(
                    _ => new EventStoreClient(
                        EventStoreClientSettings.Create(
                            "esdb://admin:changeit@localhost:2113?tls=false")))
                .AddSingleton<IEventStore, EsdbEventStore>()
                .AddSingleton<AppSettings>()
                .AddScoped<IAggregateStore, AggregateStore>()
                .AddScoped<UserService>()
                .AddScoped<UserRepository>()
                .AddScoped<ArticleRepository>()
                .AddScoped<ArticleService>()
                .AddScoped<JwtIssuer>()
                .AddScoped<IDbConnection>(o =>
                    new SqlConnection(o.GetService<IConfiguration>()
                        .GetConnectionString("ReadModels")))
                .AddSingleton<ISchemaManagement, SchemaManagement>()
                .AddCors()
                .AddControllers();

            services.AddSingleton<ICheckpointStore, SqlServerCheckpointStore>();

            services.AddSubscription<TransactionalAllStreamSubscriptionService, AllStreamSubscriptionOptions>(
                "ConduitSql",
                builder => builder
                .AddEventHandler<SqlArticleEventHandler>()
                .AddEventHandler<SqlAccountsEventHandler>()
            );

            var streamMap = new StreamNameMap();
            streamMap.Register<AccountId>(id => new StreamName($"Account-{id}"));
            streamMap.Register<ArticleId>(id => new StreamName($"Article-{id}"));
            services.AddSingleton(streamMap);
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