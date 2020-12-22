using GraphQL;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
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
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserService _currentUserService;

        public GraphQLController(ISchema schema, ICurrentUserService currentUserService,
            IDocumentExecuter documentExecuter, IDocumentWriter documentWriter,
            IModelContextBuilder modelContextBuilder,
            IEnumerable<IValidationRule> validationRules, IWebHostEnvironment env)
        {
            _currentUserService = currentUserService;
            _schema = schema;
            _documentExecuter = documentExecuter;
            _documentWriter = documentWriter;
            _modelContextBuilder = modelContextBuilder;
            _validationRules = validationRules;
            _env = env;
        }

        [HttpPost]
        [Route("/graphql")]
        [Authorize]
        public async Task<IActionResult> Index([FromBody] Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            return await ProcessQuery(query);
        }

        [HttpPost]
        [Route("/graphql-debug")]
        public async Task<IActionResult> Debug([FromBody] Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            if (_env.IsProduction())
                return Forbid("Not allowed");
            return await ProcessQuery(query);
        }

        private async Task<IActionResult> ProcessQuery(Omnikeeper.Base.Entity.GraphQLQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var trans = _modelContextBuilder.BuildImmediate();
            var user = await _currentUserService.GetCurrentUser(trans);

            var inputs = query.Variables?.ToInputs();
            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query.Query;
                options.Inputs = inputs;
                options.EnableMetrics = false;
                options.UserContext = new OmnikeeperUserContext(user);
                options.ValidationRules = DocumentValidator.CoreRules.Concat(_validationRules).ToList();
                options.RequestServices = HttpContext.RequestServices;
            });

            var json = await _documentWriter.WriteToStringAsync(result);

            if (result.Errors?.Count > 0)
            {
                return BadRequest(json);
            }

            return Ok(json);
        }
    }
}
