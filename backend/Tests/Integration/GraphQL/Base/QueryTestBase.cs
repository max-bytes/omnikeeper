using Autofac;
using Autofac.Extensions.DependencyInjection;
using GraphQL;
using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using GraphQL.Server;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddGraphQL().AddGraphTypes(typeof(GraphQLSchema));
            builder.Populate(serviceCollection);
        }

        protected IDocumentExecuter Executer { get; private set; }
        protected IDocumentWriter Writer { get; private set; }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            Inputs? inputs = null,
            object? root = null,
            IDictionary<string, object?>? userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule>? rules = null,
            IDocumentWriter? writer = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, inputs, root, userContext, cancellationToken, rules, null, writer);
        }

        public ExecutionResult AssertQueryWithErrors(
            string query,
            string expected,
            Inputs? inputs = null,
            object? root = null,
            IDictionary<string, object?>? userContext = null,
            CancellationToken cancellationToken = default,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Action<UnhandledExceptionContext>? unhandledExceptionDelegate = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQueryIgnoreErrors(
                query,
                queryResult,
                inputs,
                root,
                userContext,
                cancellationToken,
                expectedErrorCount,
                renderErrors,
                unhandledExceptionDelegate);
        }

        public ExecutionResult AssertQueryIgnoreErrors(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs? inputs = null,
            object? root = null,
            IDictionary<string, object?>? userContext = null,
            CancellationToken cancellationToken = default,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Action<UnhandledExceptionContext>? unhandledExceptionDelegate = null)
        {
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = ServiceProvider.GetRequiredService<ISchema>();
                options.Query = query;
                options.Root = root;
                options.Inputs = inputs;
                options.UserContext = userContext ?? new Dictionary<string, object?>();
                options.CancellationToken = cancellationToken;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (ctx => { });
            }).GetAwaiter().GetResult();

            var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data };

            var writtenResult = Writer.WriteToStringAsync(renderResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            writtenResult.ShouldBeCrossPlat(expectedResult);

            var errors = runResult.Errors ?? new ExecutionErrors();

            Assert.AreEqual(expectedErrorCount, errors.Count());

            return runResult;
        }

        public ExecutionResult AssertQuery(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs? inputs,
            object? root,
            IDictionary<string, object?>? userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule>? rules = null,
            Action<UnhandledExceptionContext>? unhandledExceptionDelegate = null,
            IDocumentWriter? writer = null)
        {
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = ServiceProvider.GetRequiredService<ISchema>();
                options.Query = query;
                options.Root = root;
                options.Inputs = inputs;
                options.UserContext = userContext ?? new Dictionary<string, object?>();
                options.CancellationToken = cancellationToken;
                options.ValidationRules = rules;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (ctx => { });
                options.RequestServices = ServiceProvider;
            }).GetAwaiter().GetResult();

            writer ??= Writer;

            var writtenResult = Writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            string? additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x?.InnerException?.Message));
            }

            writtenResult.ShouldBeCrossPlat(expectedResult, additionalInfo);

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
