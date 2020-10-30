using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity.GridView;
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

        private readonly IMediator _mediatr;

        public GridViewController(IMediator mediatr)
        {
            _mediatr = mediatr;
        }

        /// <summary>
        /// Returns a list of contexts for grid view.
        /// </summary>
        /// <returns>200</returns>
        [HttpGet("contexts")]
        public async Task<IActionResult> GetContexts()
        {
            var result = await _mediatr.Send(new GetContextsQuery.Query());
            return Ok(result);
        }

        /// <summary>
        /// Adds new context
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Created context</returns>
        /// <response code="201">Returns the newly created context</response>
        /// <response code="400">If creating context fails</response>  
        [HttpPost("context")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddContext([FromBody] AddContextRequest context)
        {
            var isSuccess = await _mediatr.Send(new AddContextCommand.Command { Context = context });

            if (isSuccess)
            {
                return CreatedAtAction(nameof(AddContext), new { context.Name });
            }

            return BadRequest(new { Error = "An error ocurred trying to add this context!" });
        }

        /// <summary>
        /// Edits specific context
        /// </summary>
        /// <param name="name"></param>
        /// <param name="configuration"></param>
        /// <returns>Status indication request status</returns>
        /// <response code="200">If request is successful</response>
        /// <response code="400">If editing the context fails</response>  
        [HttpPut("context/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditContext([FromRoute] string name, [FromBody] GridViewConfiguration configuration)
        {
            var isSuccess = await _mediatr.Send(new EditContextCommand.Command
            {
                Name = name,
                Configuration = configuration
            });

            if (isSuccess)
            {
                return Ok();
            }

            return BadRequest(new { Error = $"An error ocurred trying to edit context: {name}!" });

        }

        /// <summary>
        /// Deletes specific context
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Status indication request status</returns>
        /// <response code="200">If request is successful</response>
        /// <response code="400">If editing the context fails</response>  
        [HttpDelete("context/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteContext([FromRoute] string name)
        {
            var isSuccess = await _mediatr.Send(new DeleteContextCommand.Command { Name = name });

            if (isSuccess)
            {
                return Ok();
            }

            return BadRequest(new { Error = $"An error ocurred trying to delete context: {name}!" });
        }

        /// <summary>
        /// Returns grid view schema for specific context
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Returns schema object for specififc context</returns>
        /// <response code="200"></response>
        [HttpGet("contexts/{context}/schema")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchema([FromRoute] string context)
        {
            var result = await _mediatr.Send(new GetSchemaQuery.Query { Context = context });
            return Ok(result);
        }

        /// <summary>
        /// Returns grid view data for specific context
        /// </summary>
        /// <param name="context"></param>
        /// <returns>An object which contains rows for grid view</returns>
        /// <response code="200"></response>
        [HttpGet("contexts/{context}/data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetData([FromRoute] string context)
        {
            var result = await _mediatr.Send(new GetDataQuery.Query { Context = context });
            return Ok(result);
        }

        /// <summary>
        /// Saves grid view row changes and returns change results
        /// </summary>
        /// <param name="context"></param>
        /// <param name="changes"></param>
        /// <returns>A list of changes or an error</returns>
        /// <response code="200">If request is successful</response>
        /// <response code="400">If saving changes fails</response>  
        [HttpPost("contexts/{context}/change")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeData([FromRoute] string context, [FromBody] ChangeDataRequest changes)
        {
            var (result, isSuccess, error) = await _mediatr.Send(new ChangeDataCommand.Command { Changes = changes, Context = context });

            if (isSuccess)
            {
                return Ok(result);
            }

            return NotFound(new { Error = error });
        }
    }
}
