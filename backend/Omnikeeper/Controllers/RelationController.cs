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
    [Obsolete]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class RelationController : ControllerBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public RelationController(IRelationModel relationModel, ICIBasedAuthorizationService ciBasedAuthorizationService,
            ICurrentUserAccessor currentUserService, IModelContextBuilder modelContextBuilder, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.relationModel = relationModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicateID"></param>
        /// <param name="layerIDs"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        [HttpGet("getMergedRelationsWithPredicate")]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsWithPredicate([FromQuery, Required] string predicateID, [FromQuery, Required] string[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);

            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");

            var relations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(predicateID), layerset, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
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
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetAllMergedRelations([FromQuery, Required] string[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);

            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");

            var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerset, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
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
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsOutgoingFromCI([FromQuery, Required] Guid fromCIID, [FromQuery, Required] string[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");
            if (!ciBasedAuthorizationService.CanReadCI(fromCIID))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {fromCIID}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relations = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(fromCIID), layerset, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
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
        [Obsolete]
        public async Task<ActionResult<IEnumerable<RelationDTO>>> GetMergedRelationsFromOrToCI([FromQuery, Required] Guid ciid, [FromQuery, Required] string[] layerIDs, [FromQuery] DateTimeOffset? atTime = null)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");
            if (!ciBasedAuthorizationService.CanReadCI(ciid))
                return Forbid($"User \"{user.Username}\" does not have permission to read from CI {ciid}");

            var timeThreshold = (atTime.HasValue) ? TimeThreshold.BuildAtTime(atTime.Value) : TimeThreshold.BuildLatest();
            var layerset = new LayerSet(layerIDs);
            var relationsFrom = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(ciid), layerset, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
            var relationsTo = await relationModel.GetMergedRelations(RelationSelectionTo.Build(ciid), layerset, trans, timeThreshold, MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);
            var relations = relationsFrom.Concat(relationsTo);
            relations = relations.Where(r => ciBasedAuthorizationService.CanReadAllCIs(new Guid[] { r.Relation.FromCIID, r.Relation.ToCIID }, out _)); // TODO: refactor to use a method that queries all ciids at once, returning those that are readable
            return Ok(relations.Select(r => RelationDTO.BuildFromMergedRelation(r)));
        }
    }
}
