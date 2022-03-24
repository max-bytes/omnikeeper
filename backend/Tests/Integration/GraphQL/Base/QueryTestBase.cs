using Autofac;
using Autofac.Extensions.DependencyInjection;
using GraphQL;
using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using GraphQL.Server;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Tests.Integration.GraphQL.Base
{
    abstract class QueryTestBase : DIServicedTestBase
    {
        public QueryTestBase() : base(false)
        {
            Executer = new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer());
            Writer = new DocumentWriter(indent: true);

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

            global::GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(serviceCollection)
                .AddErrorInfoProvider(opt => {
                    opt.ExposeExceptionStackTrace = true;
                    opt.ExposeData = true;
                    opt.ExposeCode = true;
                    opt.ExposeCodes = true;
                })
                .AddGraphTypes(Assembly.GetAssembly(typeof(GraphQLSchema))!);

            builder.Populate(serviceCollection);
        }

        protected IDocumentExecuter Executer { get; private set; }
        protected IDocumentWriter Writer { get; private set; }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            IDictionary<string, object?> userContext,
            Inputs? inputs = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, userContext, inputs);
        }

        public (ExecutionResult result, string json) RunQuery(string query, IDictionary<string, object?> userContext, Inputs? inputs = null)
        {
            var schema = GetService<GraphQLSchemaHolder>().GetSchema();

            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query;
                options.Root = null;
                options.Inputs = inputs;
                options.UserContext = userContext;
                options.CancellationToken = default;
                options.ValidationRules = null;
                options.UnhandledExceptionDelegate = (ctx => { });
                options.RequestServices = ServiceProvider;
            }).GetAwaiter().GetResult();

            var writtenResult = Writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
            return (runResult, writtenResult);
        }

        private ExecutionResult AssertQuery(
            string query,
            ExecutionResult expectedExecutionResult,
            IDictionary<string, object?> userContext,
            Inputs? inputs)
        {
            var (runResult, writtenResult) = RunQuery(query, userContext, inputs);

            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            string? additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x?.InnerException?.Message));
            }

            writtenResult.ShouldBeCrossPlatJson(expectedResult);

            return runResult;
        }

        public static ExecutionResult CreateQueryResult(string result, ExecutionErrors? errors = null)
        {
            return new ExecutionResult
            {
                Data = string.IsNullOrWhiteSpace(result) ? null : result.ToInputs(),
                Errors = errors,
                Executed = true
            };
        }
    }
}
