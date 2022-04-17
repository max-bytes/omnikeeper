using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SystemTests.Base
{
    public abstract class TestBase
    {
        private TestcontainersContainer postgresContainer;
        private TestcontainersContainer omnikeeperContainer;
        private IDockerNetwork network;
        private GraphQLHttpClient graphQLClient;

        protected readonly string BaseUrl = "http://localhost:8080";

        protected async Task<GraphQLResponse<TResponse>> Query<TResponse>(string query, Func<TResponse> defineResponseType)
        {
            return await Query(new GraphQLRequest() { Query = query }, defineResponseType);
        }

        protected async Task<GraphQLResponse<TResponse>> Query<TResponse>(GraphQLRequest request, Func<TResponse> defineResponseType)
        {
            try
            {
                return await graphQLClient.SendQueryAsync<TResponse>(request, defineResponseType);
            } catch (GraphQLHttpRequestException e)
            {
                Assert.Fail(e.Content);
                return null;
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
                return null;
            }
        }

        [SetUp]
        public async Task SetUp()
        {
            //using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            //var logger = loggerFactory.CreateLogger<Test01>();
            //TestcontainersSettings.Logger = logger;

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
                .WithEnvironment("Authentication__debugAllowAll", "true")
                .WithEnvironment("Authorization__debugAllowAll", "true")
                .WithEnvironment("ShowPII", "true")
                .WithNetwork(network)
                .WithPortBinding(8080, 80)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
                .Build();
            await omnikeeperContainer.StartAsync();

            graphQLClient = new GraphQLHttpClient($"{BaseUrl}/graphql", new SystemTextJsonSerializer());

            Thread.Sleep(2000); // wait a bit more to make sure omnikeeper is fully initialized (f.e. trait rebuilding)
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
            if (graphQLClient != null)
                graphQLClient.Dispose();
        }
    }
}
