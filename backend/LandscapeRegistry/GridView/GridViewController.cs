using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LandscapeRegistry.GridView.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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


        [AllowAnonymous]
        [HttpGet("predicate/{id}")]
        public async Task<IActionResult> GetPredicate(int id)
        {
            var res = await _mediatr.Send(new GetPredicatesQuery { PredicateId = id });
            return Ok(res);
        }
    }
}
