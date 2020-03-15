using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Landscape.Base.Model;
using LandscapePrototype.Entity;
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
        private readonly IUserModel _userModel;

        public GraphQLController(ISchema schema, IUserModel userModel,
            IDocumentExecuter documentExecuter,
            IEnumerable<IValidationRule> validationRules, IWebHostEnvironment env)
        {
            _userModel = userModel;
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

            var user = await GetUser(HttpContext);

            var inputs = query.Variables?.ToInputs();
            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query.Query;
                options.Inputs = inputs;
                options.UserContext = new LandscapeUserContext(user);
                options.ValidationRules = DocumentValidator.CoreRules.Concat(_validationRules).ToList();
            });

            if (result.Errors?.Count > 0)
                return BadRequest(result);

            return Ok(result);
        }

        private async Task<User> GetUser(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var claims = httpContext.User.Claims;
            // TODO: check if this works or is a hack, with a magic string
            var username = claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
            var guidString = claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var groups = claims.Where(c => c.Type == "groups").Select(c => c.Value).ToArray();

            var usertype = Entity.UserType.Unknown;
            if (groups.Contains("/humans"))
                usertype = Entity.UserType.Human;
            else if (groups.Contains("/robots"))
                usertype = Entity.UserType.Robot;

            if (username == null)
            {
                var anonymouseGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef2"); // TODO: load from claims
                return Entity.User.Build(-1L, anonymouseGuid, "anonymous", Entity.UserType.Unknown, DateTimeOffset.Now);
            }
            var guid = new Guid(guidString); 
            return await _userModel.CreateOrUpdateFetchUser(username, guid, usertype, null);
        }
    }
}
