using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DbUp;
using Eventuous.Projections.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Conduit.ReadModels
{
    public class SchemaManagement : ISchemaManagement
    {
        private readonly IConfiguration _configuration;

        private string ConnectionString =>
            _configuration.GetConnectionString("ReadModels");

        private SqlConnection MasterConnection =>
            new SqlConnection(_configuration.GetConnectionString("Master"));

        public SchemaManagement(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static bool _created;

        public async Task CreateSchemaOnce()
        {
            if (_created) return;

            await EnsureDatabase();

            var upgradeEngine =
                DeployChanges.To.SqlDatabase(ConnectionString)
                    .WithScriptsEmbeddedInAssembly(GetType().Assembly)
                    .LogToConsole()
                    .Build();

            upgradeEngine.PerformUpgrade();

            _created = true;
        }

        private async Task EnsureDatabase()
        {
            await using var connection = MasterConnection;
            await TryConnect(connection);
            const string query = @"
if not exists(select *
          from sys.databases
          where name = 'conduit')
    begin
        create database conduit;
    end
";
            await connection.ExecuteAsync(query);
            Console.WriteLine("Created Conduit database.");

            async Task TryConnect(IDbConnection sqlConnection)
            {
                for (var i = 0; i < 100; i++)
                {
                    try
                    {
                        await sqlConnection.QueryAsync("select 1");
                    }
                    catch
                    {
                        Console.WriteLine($"Login attempt {i} failed.");
                        await Task.Delay(1000);
                    }
                }
            }
        }
    }
}