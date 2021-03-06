using System.Data;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Accounts.Events;
using Conduit.Api.Features.Accounts.Projectors;
using Conduit.Api.Features.Accounts.Queries;
using Conduit.Api.Features.Articles;
using Conduit.Api.Features.Articles.Events;
using Conduit.Api.Features.Articles.Projectors;
using Conduit.Api.Features.Articles.Queries;
using Conduit.ReadModels;
using EventStore.Client;
using Eventuous;
using Eventuous.EventStoreDB;
using Eventuous.Projections.SqlServer;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.EventStoreDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                .AddScoped<IDbConnection>(o =>
                    new SqlConnection(o.GetService<IConfiguration>()
                        .GetConnectionString("ReadModels")))
                .AddSingleton<ISchemaManagement, SchemaManagement>()
                .AddCors()
                .AddControllers();

            AccountsRegistration.Register();
            ArticlesRegistration.Register();

            services.AddSingleton<ICheckpointStore, SqlServerCheckpointStore>();

            services.AddSingleton(o =>
                new TransactionalAllStreamSubscriptionService(
                    o.GetService<EventStoreClient>()!,
                    new AllStreamSubscriptionOptions
                        {SubscriptionId = "ConduitSql"},
                    o.GetService<ICheckpointStore>()!, new IEventHandler[]
                    {
                        new SqlArticleEventHandler(
                            o.GetService<IConfiguration>()!, "ConduitSql",
                            o.GetService<ILoggerFactory>()!),
                        new SqlAccountsEventHandler(
                            o.GetService<IConfiguration>()!, "ConduitSql",
                            o.GetService<ILoggerFactory>()!)
                    }, o.GetService<IEventSerializer>()!,
                    o.GetService<ILoggerFactory>()));

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