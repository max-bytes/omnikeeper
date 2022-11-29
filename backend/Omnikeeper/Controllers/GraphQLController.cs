using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("[controller]")]
    public class GraphQLController : Controller // scoped lifetime
    {
        private GraphQLSchemaHolder _schemaHolder;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly IGraphQLTextSerializer _serializer;
        private readonly IScopedUsageTracker usageTracker;
        private readonly IModelContextBuilder _modelContextBuilder;
        private readonly DataLoaderDocumentListener dataLoaderDocumentListener;
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserAccessor _currentUserService;

        public GraphQLController(ICurrentUserAccessor currentUserService, GraphQLSchemaHolder schemaHolder,
            IDocumentExecuter documentExecuter, IGraphQLTextSerializer serializer, IScopedUsageTracker usageTracker,
            IModelContextBuilder modelContextBuilder, DataLoaderDocumentListener dataLoaderDocumentListener,
            IEnumerable<IValidationRule> validationRules, IWebHostEnvironment env)
        {
            _schemaHolder = schemaHolder;
            _currentUserService = currentUserService;
            _documentExecuter = documentExecuter;
            _serializer = serializer;
            this.usageTracker = usageTracker;
            _modelContextBuilder = modelContextBuilder;
            this.dataLoaderDocumentListener = dataLoaderDocumentListener;
            _validationRules = validationRules;
            _env = env;
        }

        [HttpPost]
        [Route("/graphql")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [UseSpanJson]
        public async Task<IActionResult> Index([FromBody] Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            return await ProcessQuery(query);
        }

        [HttpPost]
        [Route("/graphql-debug")]
        [UseSpanJson]
        public async Task<IActionResult> Debug([FromBody] Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            if (_env.IsProduction())
                return Forbid("Not allowed");
            return await ProcessQuery(query);
        }

        // NOTE: be aware of https://github.com/dotnet/aspnetcore/issues/37360
        [HttpGet]
        [Route("/graphql")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [UseSpanJson]
        public async Task<IActionResult> Get([FromQuery] Omnikeeper.Base.Entity.GraphQLQuery q)
        {
            return await ProcessQuery(q);
        }

        private async Task<IActionResult> ProcessQuery(Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var trans = _modelContextBuilder.BuildImmediate();
            var user = await _currentUserService.GetCurrentUser(trans);

            var schema = _schemaHolder.GetSchema();

            using var userContext = new OmnikeeperUserContext(user, HttpContext.RequestServices);

            var inputs = query.Variables?.ToInputs();
            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query.Query;
                options.Variables = inputs;
                options.EnableMetrics = false;
                options.UserContext = userContext;
                options.ValidationRules = DocumentValidator.CoreRules.Concat(_validationRules).ToList();
                options.RequestServices = HttpContext.RequestServices;
                options.Listeners.Add(dataLoaderDocumentListener);
                options.Listeners.Add(new UserContextPreparationDocumentExecutionListener());
                options.Listeners.Add(new UsageTrackingDocumentExecutionListener(usageTracker));
            });

            var json = _serializer.Serialize(result);

            var ret = Content(json, "application/json");
            if (result.Errors?.Count > 0)
            {
                ret.StatusCode = 400;
            }

            return ret;
        }
    }

    public class UserContextPreparationDocumentExecutionListener : IDocumentExecutionListener
    {
        public Task AfterExecutionAsync(IExecutionContext context)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task BeforeExecutionAsync(IExecutionContext context)
        {
            var uc = (context.UserContext as OmnikeeperUserContext)!;
            var timeThreshold = TimeThreshold.BuildLatest();
            uc.WithTimeThreshold(timeThreshold, new List<object>());
            switch (context.Operation.Operation)
            {
                case GraphQLParser.AST.OperationType.Query:
                    uc.WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());
                    break;
                case GraphQLParser.AST.OperationType.Mutation:
                    uc.WithTransaction(modelContextBuilder => modelContextBuilder.BuildDeferred())
                        .WithChangesetProxy(context.RequestServices!.GetRequiredService<IChangesetModel>(), timeThreshold, new DataOriginV1(DataOriginType.Manual));
                    break;
                case GraphQLParser.AST.OperationType.Subscription:
                    throw new Exception("Unsupported");
            }

            return Task.CompletedTask;
        }
    }

    public class UsageTrackingDocumentExecutionListener : IDocumentExecutionListener
    {
        private readonly IScopedUsageTracker usageTracker;

        public UsageTrackingDocumentExecutionListener(IScopedUsageTracker usageTracker)
        {
            this.usageTracker = usageTracker;
        }

        public Task AfterExecutionAsync(IExecutionContext context)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
        {
            // NO-OP
            return Task.CompletedTask;
        }

        public Task BeforeExecutionAsync(IExecutionContext context)
        {
            switch (context.Operation.Operation)
            {
                case GraphQLParser.AST.OperationType.Query:
                    foreach(var selection in context.Operation.SelectionSet.Selections)
                        if (selection is GraphQLField field)
                        {
                            var operationName = field.Name.StringValue;
                            if (operationName == "traitEntities")
                            { // special handling for trait entities, as the important information (which trait is being queried) sits one level deeper
                                if (field.SelectionSet != null)
                                    foreach(var teSelection in field.SelectionSet.Selections)
                                        if (teSelection is GraphQLField teField)
                                            usageTracker.TrackUseGraphQLOperation($"traitEntities.{teField.Name.StringValue}", Base.Entity.UsageStatsOperation.Read);
                            } else
                            {
                                usageTracker.TrackUseGraphQLOperation(operationName, Base.Entity.UsageStatsOperation.Read);
                            }
                        }
                    break;
                case GraphQLParser.AST.OperationType.Mutation:
                    foreach (var selection in context.Operation.SelectionSet.Selections)
                        if (selection is GraphQLField field)
                            usageTracker.TrackUseGraphQLOperation(field.Name.StringValue, Base.Entity.UsageStatsOperation.Write);
                    break;
                case GraphQLParser.AST.OperationType.Subscription:
                    throw new Exception("Unsupported");
            }

            return Task.CompletedTask;
        }
    }
}
