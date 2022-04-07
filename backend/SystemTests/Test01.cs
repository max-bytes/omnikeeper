using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.SystemTextJson;
using System;
using System.Diagnostics;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Networks.Builders;
using DotNet.Testcontainers.Networks.Configurations;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.WaitStrategies;

namespace SystemTests
{
    public class Test01
    {
        private TestcontainersContainer postgresContainer;
        private TestcontainersContainer omnikeeperContainer;
        private IDockerNetwork network;

        [Test]
        public async Task TestBasics()
        {
            var ciidsRequest = new GraphQLRequest
            {
                Query = @"
                {
                    ciids
                }"
            };
            var graphQLClient = new GraphQLHttpClient("http://localhost:8080/graphql", new SystemTextJsonSerializer());
            var graphQLResponse = await graphQLClient.SendQueryAsync(ciidsRequest, () => new { ciids = new List<Guid>() });

            Assert.IsNull(graphQLResponse.Errors);
            Assert.AreEqual(0, graphQLResponse.Data.ciids.Count);
        }

        [SetUp]
        public async Task SetUp()
        {
            //using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            //var logger = loggerFactory.CreateLogger<Test01>();
            //TestcontainersSettings.Logger = logger;

            // TODO: needed?
            network = new TestcontainersNetworkBuilder()
                .WithDriver(NetworkDriver.Bridge)
                .WithName("omnikeeper-system-tests")
                .Build();
            await network.CreateAsync();

            postgresContainer = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("postgres:12")
                .WithName("database")
                .WithEnvironment("POSTGRES_DB", "omnikeeper")
                .WithEnvironment("POSTGRES_USER", "postgres")
                .WithEnvironment("POSTGRES_PASSWORD", "postgres")
                .WithNetwork(network)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();

            await postgresContainer.StartAsync();

            omnikeeperContainer = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("ghcr.io/max-bytes/omnikeeper/variants/backend/internal:latest")
                .WithName("omnikeeper")
                .WithEnvironment("ConnectionStrings__OmnikeeperDatabaseConnection", $"Server=database; User Id=postgres; Password=postgres; Database=omnikeeper; Port=5432; Pooling = true; Keepalive = 1024; Timeout = 1024; CommandTimeout = 1024")
                .WithEnvironment("ConnectionStrings__QuartzDatabaseConnection", $"Server=database; User Id=postgres; Password=postgres; Database=omnikeeper; Port=5432; Pooling = true; Keepalive = 1024; Timeout = 1024; CommandTimeout = 1024")
                .WithEnvironment("Authentication__Audience", "omnikeeper")
                .WithEnvironment("Authentication__Authority", "http://keycloak:8080/auth/realms/omnikeeper")
                .WithEnvironment("Authentication__ValidateIssuer", "false") // TODO: needed?
                .WithEnvironment("Authentication__debugAllowAll", "true")
                .WithEnvironment("Authorization__debugAllowAll", "true")
                .WithEnvironment("CORS__AllowedHosts", "") // TODO: needed?
                .WithEnvironment("ShowPII", "true")
                .WithNetwork(network)
                .WithPortBinding(8080, 80)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
                .Build();
            await omnikeeperContainer.StartAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            if (omnikeeperContainer != null)
                await omnikeeperContainer.DisposeAsync();
            if (postgresContainer != null)
                await postgresContainer.DisposeAsync();
            if (network != null)
                await network.DeleteAsync();
        }
    }
}
