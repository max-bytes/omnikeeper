using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using LandscapePrototype.Entity.GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LandscapePrototype.Controllers
{
    public class GraphQLQuery
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public JObject Variables { get; set; }
    }

    [ApiController]
    public class GraphQLController : Controller
    {
        private readonly ISchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IWebHostEnvironment _env;

        public GraphQLController(ISchema schema,
            IDocumentExecuter documentExecuter,
            IEnumerable<IValidationRule> validationRules, IWebHostEnvironment env)
        {
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

            var inputs = query.Variables?.ToInputs();
            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query.Query;
                options.Inputs = inputs;
                options.UserContext = new LandscapeUserContext(HttpContext);
                options.ValidationRules = DocumentValidator.CoreRules.Concat(_validationRules).ToList();
            });

            if (result.Errors?.Count > 0)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
