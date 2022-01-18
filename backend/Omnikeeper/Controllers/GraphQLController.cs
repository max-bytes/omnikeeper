using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using Omnikeeper.GraphQL.TraitEntities;
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
    public class GraphQLController : Controller
    {
        private readonly ISchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly IDocumentWriter _documentWriter;
        private readonly IModelContextBuilder _modelContextBuilder;
        private readonly DataLoaderDocumentListener dataLoaderDocumentListener;
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IWebHostEnvironment _env;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ITraitsProvider traitsProvider;
        private readonly TraitEntitiesQuerySchemaLoader traitEntitiesQuerySchemaLoader;
        private readonly TraitEntitiesMutationSchemaLoader traitEntitiesMutationSchemaLoader;
        private readonly ElementTypesContainerCreator elementTypesContainerCreator;
        private readonly ILogger<GraphQLController> logger;
        private readonly ICurrentUserAccessor _currentUserService;

        public GraphQLController(ISchema schema, ICurrentUserAccessor currentUserService,
            IDocumentExecuter documentExecuter, IDocumentWriter documentWriter,
            IModelContextBuilder modelContextBuilder, DataLoaderDocumentListener dataLoaderDocumentListener,
            IEnumerable<IValidationRule> validationRules, IWebHostEnvironment env,
            IHostApplicationLifetime appLifetime, ITraitsProvider traitsProvider,
            TraitEntitiesQuerySchemaLoader traitEntitiesTypeLoader, TraitEntitiesMutationSchemaLoader traitEntitiesMutationSchemaLoader,
            ILogger<GraphQLController> logger, ElementTypesContainerCreator elementTypesContainerCreator)
        {
            _currentUserService = currentUserService;
            _schema = schema;
            _documentExecuter = documentExecuter;
            _documentWriter = documentWriter;
            _modelContextBuilder = modelContextBuilder;
            this.dataLoaderDocumentListener = dataLoaderDocumentListener;
            _validationRules = validationRules;
            _env = env;
            this.appLifetime = appLifetime;
            this.traitsProvider = traitsProvider;
            this.traitEntitiesQuerySchemaLoader = traitEntitiesTypeLoader;
            this.traitEntitiesMutationSchemaLoader = traitEntitiesMutationSchemaLoader;
            this.logger = logger;
            this.elementTypesContainerCreator = elementTypesContainerCreator;
        }

        [HttpPost]
        [Route("/graphql")]
        [Authorize]
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

        [HttpGet("/blow-me-up")]
        public IActionResult BlowMeUp()
        {
            appLifetime.StopApplication();
            return new EmptyResult();
        }

        private static readonly Object traitEntitiesInitLock = new Object();

        private async Task<IActionResult> ProcessQuery(Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var trans = _modelContextBuilder.BuildImmediate();
            var user = await _currentUserService.GetCurrentUser(trans);

            // TODO: we should only call *.Init() on app startup and after trait changes... NOT on every request
            if (!_schema.Initialized)
            {
                try
                {
                    var timeThreshold = TimeThreshold.BuildLatest();
                    var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);
                    lock (traitEntitiesInitLock)
                    {
                        var typesContainers = elementTypesContainerCreator.CreateTypes(activeTraits, _schema, logger);
                        traitEntitiesQuerySchemaLoader.Init(typesContainers);
                        traitEntitiesMutationSchemaLoader.Init(typesContainers);
                    }
                } catch(Exception e)
                {
                    logger.LogError(e, "Encountered error while creating trait entity GraphQL schema");
                }
            }

            var inputs = query.Variables?.ToInputs();
            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query.Query;
                options.Inputs = inputs;
                options.EnableMetrics = false;
                options.UserContext = new OmnikeeperUserContext(user, HttpContext.RequestServices);
                options.ValidationRules = DocumentValidator.CoreRules.Concat(_validationRules).ToList();
                options.RequestServices = HttpContext.RequestServices;
                options.Listeners.Add(dataLoaderDocumentListener);
            });

            var json = await _documentWriter.WriteToStringAsync(result);

            var ret = Content(json, "application/json");
            if (result.Errors?.Count > 0)
            {
                ret.StatusCode = 400;
            }

            return ret;
        }
    }
}
