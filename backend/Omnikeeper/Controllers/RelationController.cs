using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class RelationController : ControllerBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICurrentUserService currentUserService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;

        public RelationController(IRelationModel relationModel, ICIBasedAuthorizationService ciBasedAuthorizationService,
            ICurrentUserService currentUserService, IModelContextBuilder modelContextBuilder)
        {
            this.relationModel = relationModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromCIID"></param>
        /// <param name="toCIID"></param>
        /// <param name="predicateID"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelation")]
        public async Task<ActionResult<RelationDTO>> GetMergedRelation([FromQuery, Required] Guid fromCIID, [FromQuery, Required] Guid toCIID, [FromQuery, Required] string predicateID, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!ciBasedAuthorizationService.CanReadAllCIs(new Guid[] { fromCIID, toCIID }, out var notAllowedCI))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {notAllowedCI}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relation = await relationModel.GetMergedRelation(fromCIID, toCIID, predicateID, layerset, trans, timeThreshold);
            if (relation == null) return NotFound();
            return Ok(RelationDTO.BuildFromMergedRelation(relation));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicateID"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsWithPredicate")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsWithPredicate([FromQuery, Required] string predicateID, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID), layerset, trans, timeThreshold);
            relations = relations.Where(r => ciBasedAuthorizationService.CanReadAllCIs(new Guid[] { r.Relation.FromCIID, r.Relation.ToCIID }, out _));
            return Ok(relations.Select(r => RelationDTO.BuildFromMergedRelation(r)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getAllMergedRelations")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetAllMergedRelations([FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionAll(), layerset, trans, timeThreshold);
            relations = relations.Where(r => ciBasedAuthorizationService.CanReadAllCIs(new Guid[] { r.Relation.FromCIID, r.Relation.ToCIID }, out _));
            return Ok(relations.Select(r => RelationDTO.BuildFromMergedRelation(r)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromCIID"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsOutgoingFromCI")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsOutgoingFromCI([FromQuery, Required] Guid fromCIID, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!ciBasedAuthorizationService.CanReadCI(fromCIID))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {fromCIID}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionFrom(fromCIID), layerset, trans, timeThreshold);
            relations = relations.Where(r => ciBasedAuthorizationService.CanReadCI(r.Relation.ToCIID)); // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
            return Ok(relations.Select(r => RelationDTO.BuildFromMergedRelation(r)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ciid"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsFromOrToCI")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsFromOrToCI([FromQuery, Required] Guid ciid, [FromQuery, Required] long[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!ciBasedAuthorizationService.CanReadCI(ciid))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {ciid}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionEitherFromOrTo(ciid), layerset, trans, timeThreshold);
            relations = relations.Where(r => ciBasedAuthorizationService.CanReadAllCIs(new Guid[] { r.Relation.FromCIID, r.Relation.ToCIID }, out _)); // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
            return Ok(relations.Select(r => RelationDTO.BuildFromMergedRelation(r)));
        }
    }
}
