using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class RelationController : ControllerBase
    {
        private readonly IRelationModel relationModel;

        public RelationController(IRelationModel relationModel)
        {
            this.relationModel = relationModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicateID"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsWithPredicate")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsWithPredicate([FromQuery, Required]string predicateID, [FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID), layerset, null, timeThreshold);
            return Ok(relations.Select(r => RelationDTO.Build(r)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getAllMergedRelations")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetAllMergedRelations([FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionAll(), layerset, null, timeThreshold);
            return Ok(relations.Select(r => RelationDTO.Build(r)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromCIID"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsOutgoingFromCI")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsOutgoingFromCI([FromQuery, Required]Guid fromCIID, [FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionFrom(fromCIID), layerset, null, timeThreshold);
            return Ok(relations.Select(r => RelationDTO.Build(r)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ciid"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsFromOrToCI")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsFromOrToCI([FromQuery, Required]Guid ciid, [FromQuery, Required]long[] layerIDs, [FromQuery]DateTimeOffset? atTime = null)
        {
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionEitherFromOrTo(ciid), layerset, null, timeThreshold);
            return Ok(relations.Select(r => RelationDTO.Build(r)));
        }
    }
}
