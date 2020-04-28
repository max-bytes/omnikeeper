using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;
using LandscapeRegistry.GraphQL;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers
{
    public class GraphQLQuery
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public JObject Variables { get; set; }
    }

    [ApiController]
    [ApiVersionNeutral]
    [Route("[controller]")]
    public class GraphQLController : Controller
    {
        private readonly ISchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentUserService _currentUserService;

        public GraphQLController(ISchema schema, ICurrentUserService currentUserService,
            IDocumentExecuter documentExecuter,
            IEnumerable<IValidationRule> validationRules, IWebHostEnvironment env)
        {
            _currentUserService = currentUserService;
            _schema = schema;
            _documentExecuter = documentExecuter;
            _validationRules = validationRules;
            _env = env;
        }

        [HttpPost]
        [Route("/graphql")]
        [Authorize]
        public async Task<IActionResult> Index([FromBody] GraphQLQuery query)
        {
            return await ProcessQuery(query);
        }

        [HttpPost]
        [Route("/graphql-debug")]
        public async Task<IActionResult> Debug([FromBody] GraphQLQuery query)
        {
            if (!_env.IsDevelopment())
                return BadRequest("Not allowed");
            return await ProcessQuery(query);
        }

        private async Task<IActionResult> ProcessQuery(GraphQLQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var user = await _currentUserService.GetCurrentUser(null);

            var inputs = query.Variables?.ToInputs();
            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query.Query;
                options.Inputs = inputs;
                options.UserContext = new RegistryUserContext(user);
                options.ValidationRules = DocumentValidator.CoreRules.Concat(_validationRules).ToList();
                options.ExposeExceptions = _env.IsDevelopment();
            });

            //if (result.Errors?.Count > 0)
            //{
            //    return BadRequest(result);
            //}

            return Ok(result);
        }
    }
}
