using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.GridView.Commands;
using Omnikeeper.GridView.Queries;
using Omnikeeper.GridView.Request;

namespace LandscapeRegistry.GridView
{
    //[Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GridViewController : ControllerBase
    {
        // TO DO: api versioning

        private readonly IMediator _mediatr;

        public GridViewController(IMediator mediatr)
        {
            _mediatr = mediatr;
        }

        /// <summary>
        /// Returns a list of contexts for grid view.
        /// </summary>
        /// <returns>200</returns>
        [AllowAnonymous]
        [HttpGet("contexts")]
        public async Task<IActionResult> GetContexts()
        {
            var result = await _mediatr.Send(new GetContextsQuery.Query());
            return Ok(result);
        }

        /// <summary>
        /// Returns grid view schema for specific context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>200</returns>
        [AllowAnonymous]
        [HttpGet("contexts/{context}/schema")]
        public async Task<IActionResult> GetSchema([FromRoute] string context)
        {
            var result = await _mediatr.Send(new GetSchemaQuery.Query { Context = context });
            return Ok(result);
        }

        /// <summary>
        /// Returns grid view data for specific context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>200</returns>
        [AllowAnonymous]
        [HttpGet("contexts/{context}/data")]
        public async Task<IActionResult> GetData([FromRoute] string context)
        {
            var result = await _mediatr.Send(new GetDataQuery.Query { Context = context });
            return Ok(result);
        }

        /// <summary>
        /// Saves grid view row changes and returns change results.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="changes"></param>
        /// <returns>200</returns>
        [AllowAnonymous]
        [HttpPost("contexts/{context}/data")]
        public async Task<IActionResult> ChangeData([FromRoute] string context, [FromBody] ChangeDataRequest changes)
        {
            var (result, isSuccess) = await _mediatr.Send(new ChangeDataCommand.Command { Changes = changes, Context = context });

            if (isSuccess)
            {
                return Ok(result);
            }

            return NotFound(new { Error = "The provided ci id not found!" });
        }
    }
}
