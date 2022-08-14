using Autofac;
using Autofac.Extensions.DependencyInjection;
using GraphQL;
using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Controllers;
using Omnikeeper.GraphQL;
using System.Reflection;
using System.Threading.Tasks;

namespace Tests.Integration.GraphQL.Base
{
    abstract class QueryTestBase : DIServicedTestBase
    {
        public QueryTestBase() : base(false, true)
        {
            DBSetup.Setup();
        }

        protected async Task ReinitSchema()
        {
            // force rebuild graphql schema
            using var trans = ModelContextBuilder.BuildDeferred();
            var timeThreshold = TimeThreshold.BuildLatest();
            var activeTraits = await GetService<ITraitsProvider>().GetActiveTraits(trans, timeThreshold);
            GetService<GraphQLSchemaHolder>().ReInitSchema(ServiceProvider, activeTraits, NullLogger.Instance);
            trans.Commit();
        }

        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            var serviceCollection = new ServiceCollection();

            global::GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(serviceCollection, c =>
             {
                 c.AddErrorInfoProvider(opt =>
                 {
                     opt.ExposeExceptionStackTrace = true;
                     opt.ExposeData = true;
                     opt.ExposeCode = true;
                     opt.ExposeCodes = true;
                 })
                 .AddDocumentExecuter((sp) => new MyDocumentExecutor())
                 .AddGraphTypes(Assembly.GetAssembly(typeof(GraphQLSchema))!)
                 .AddSerializer<SpanJSONGraphQLSerializer>();
             });

            builder.Populate(serviceCollection);
        }

        public void AssertQuerySuccess(
            string query,
            string expected,
            AuthenticatedUser user,
            Inputs? inputs = null)
        {
            // wrap expectedResult in data object
            var expectedResult = $"{{ \"data\": {expected} }}";

            var (runResult, writtenResult) = RunQuery(query, user, inputs);

            //string? additionalInfo = null;

            //if (runResult.Errors?.Any() == true)
            //{
            //    additionalInfo = string.Join(Environment.NewLine, runResult.Errors
            //        .Where(x => x.InnerException is GraphQLSyntaxErrorException)
            //        .Select(x => x?.InnerException?.Message));
            //}

            writtenResult.ShouldBeCrossPlatJson(expectedResult);
        }

        public void AssertQueryHasErrors(
            string query,
            AuthenticatedUser user,
            Inputs? inputs = null)
        {
            var (runResult, _) = RunQuery(query, user, inputs);

            Assert.NotNull(runResult.Errors);
            Assert.Greater(runResult.Errors!.Count, 0);
        }

        public (ExecutionResult result, string json) RunQuery(string query, AuthenticatedUser user, Inputs? inputs = null)
        {
            var schema = GetService<GraphQLSchemaHolder>().GetSchema();
            var dataLoaderDocumentListener = GetService<DataLoaderDocumentListener>();
            var executor = GetService<IDocumentExecuter>();

            using var userContext = new OmnikeeperUserContext(user, ServiceProvider);

            var runResult = executor.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query;
                options.Root = null;
                options.Variables = inputs;
                options.UserContext = userContext;
                options.CancellationToken = default;
                options.ValidationRules = null;
                options.RequestServices = ServiceProvider;
                options.Listeners.Add(dataLoaderDocumentListener);
                options.Listeners.Add(new MyDocumentExecutionListener());
            }).GetAwaiter().GetResult();

            var serializer = GetService<IGraphQLTextSerializer>();
            var writtenResult = serializer.Serialize(runResult);
            return (runResult, writtenResult);
        }
    }
}
