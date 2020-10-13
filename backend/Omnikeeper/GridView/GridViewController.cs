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
        public async Task<IActionResult> GetData()
        {
            var result = await _mediatr.Send(new GetDataQuery.Query());
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPut]
        public async Task<IActionResult> ChangeData([FromBody] ChangeDataRequest changes)
        {
            var result = await _mediatr.Send(new ChangeDataCommand.Command { Changes = changes });
            return Ok(result);
        }
    }
}
