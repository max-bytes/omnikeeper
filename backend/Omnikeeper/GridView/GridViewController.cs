using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LandscapeRegistry.GridView.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.GridView.Commands;
using Omnikeeper.GridView.Queries;
using Omnikeeper.GridView.Request;

namespace LandscapeRegistry.GridView
{
    [Route("api/[controller]")]
    [ApiController]
    public class GridViewController : ControllerBase
    {
        private readonly IMediator _mediatr;

        public GridViewController(IMediator mediatr)
        {
            _mediatr = mediatr;
        }

        // test endpoint
        [AllowAnonymous]
        [HttpGet("predicate/{id}")]
        public async Task<IActionResult> GetPredicate(int id)
        {
            var res = await _mediatr.Send(new GetPredicatesQuery { PredicateId = id });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("schema")]
        public async Task<IActionResult> GetSchema()
        {
            var result = await _mediatr.Send(new GetSchemaQuery.Query());
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("data")]
        public async Task<IActionResult> GetData([FromQuery] string configurationName)
        {
            var result = await _mediatr.Send(new GetDataQuery.Query { ConfigurationName = configurationName });
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ChangeData([FromBody] ChangeDataRequest changes, [FromQuery] string configurationName)
        {
            var result = await _mediatr.Send(new ChangeDataCommand.Command { Changes = changes , ConfigurationName = configurationName });
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("contexts")]
        public async Task<IActionResult> GetContexts()
        {
            var result = await _mediatr.Send(new GetContextsQuery.Query());
            return Ok(result);
        }
    }
}
