﻿using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Eventuous.Projections.SqlServer
{
    public class SchemaManagement
    {
        private readonly IConfiguration _configuration;

        private SqlConnection Connection =>
            new SqlConnection(_configuration.GetConnectionString("ReadModels"));

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
            await EnsureCheckpoints();
            await EnsureAccounts();
            await EnsureFollowers();

            _created = true;
        }

        private async Task EnsureFollowers()
        {
            const string query = @"
if not exists(select *
          from conduit.INFORMATION_SCHEMA.TABLES
          where TABLE_NAME = 'Followers')
    begin
        create table dbo.Followers
        (
            FollowedUserId  varchar(32) not null,
            FollowingUserId varchar(32) not null,
            constraint PK_Followers primary key (FollowedUserId, FollowingUserId)
        )
    end
";
            await using var connection = Connection;
            await connection.ExecuteAsync(query);
            Console.WriteLine("Created followers.");
        }

        private async Task EnsureAccounts()
        {
            const string query = @"
if not exists(select *
          from conduit.INFORMATION_SCHEMA.TABLES
          where TABLE_NAME = 'Accounts')
    begin
        create table dbo.Accounts
        (
            StreamId        varchar(32)     not null,
            Email           varchar(200)    not null,
            Username        varchar(50)     not null,
            PasswordHash    varchar(200)    not null,
            Bio             varchar(1000)   null,
            Image           varchar(200)    null,
            constraint PK_Accounts primary key (StreamId)
        )
    end
";
            await using var connection = Connection;
            await connection.ExecuteAsync(query);
            Console.WriteLine("Created accounts.");
        }

        private async Task EnsureCheckpoints()
        {
            const string query = @"
if not exists(select *
          from conduit.INFORMATION_SCHEMA.TABLES
          where TABLE_NAME = 'Checkpoints')
    begin
        create table dbo.Checkpoints
        (
            Id       varchar(200) not null,
            Position bigint       null,
            constraint PK_Checkpoints primary key (Id)
        )
    end
";
            await using var connection = Connection;
            await connection.ExecuteAsync(query);
            Console.WriteLine("Created checkpoints table.");
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