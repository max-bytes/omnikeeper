using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.OData
{
    public class RelationDTO
    {
        public RelationDTO(Guid fromCIID, Guid toCIID, string predicate)
        {
            FromCIID = fromCIID;
            ToCIID = toCIID;
            Predicate = predicate;
        }

        [Key]
        public Guid FromCIID { get; set; }
        [Key]
        public Guid ToCIID { get; set; }
        [Key]
        public string Predicate { get; set; }
    }

    // TODO: ci based authorization
    // TODO: layer based authorization
    //[Authorize]
    public class RelationsController : ODataController
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IChangesetModel changesetModel;
        private readonly IODataAPIContextModel oDataAPIContextModel;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ICurrentUserAccessor currentUserService;
        private readonly ILayerBasedAuthorizationService authorizationService;

        public RelationsController(IRelationModel relationModel, ICIModel ciModel, IChangesetModel changesetModel, IODataAPIContextModel oDataAPIContextModel,
            ICurrentUserAccessor currentUserService, ILayerBasedAuthorizationService authorizationService, IModelContextBuilder modelContextBuilder)
        {
            this.relationModel = relationModel;
            this.ciModel = ciModel;
            this.changesetModel = changesetModel;
            this.oDataAPIContextModel = oDataAPIContextModel;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
            this.modelContextBuilder = modelContextBuilder;
        }

        private RelationDTO Model2DTO(MergedRelation r)
        {
            return new RelationDTO(r.Relation.FromCIID, r.Relation.ToCIID, r.Relation.PredicateID);
        }

        [EnableQuery]
        public async Task<RelationDTO> GetRelationDTO([FromODataUri, Required] Guid keyFromCIID, [FromODataUri, Required] Guid keyToCIID, [FromODataUri, Required] string keyPredicate, [FromRoute] string context)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var timeThreshold = TimeThreshold.BuildLatest();
            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);
            var r = await relationModel.GetMergedRelation(keyFromCIID, keyToCIID, keyPredicate, layerset, trans, timeThreshold);
            if (r == null)
                throw new Exception("Could not get relation");
            return Model2DTO(r);
        }

        [EnableQuery]
        public async Task<IEnumerable<RelationDTO>> GetRelations([FromRoute] string context)
        {
            var trans = modelContextBuilder.BuildImmediate();
            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);
            var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerset, trans, TimeThreshold.BuildLatest());

            return relations.Select(r => Model2DTO(r));
        }

        [EnableQuery]
        public async Task<IActionResult> Post([FromBody] RelationDTO relation, [FromRoute] string context)
        {
            if (relation == null)
                return BadRequest($"Could not parse inserted relation");
            if (relation.FromCIID == Guid.Empty)
                return BadRequest($"Relation from CIID must be set");
            if (relation.ToCIID == Guid.Empty)
                return BadRequest($"Relation to CIID must be set");
            if (relation.Predicate == null || relation.Predicate == "")
                return BadRequest($"Relation Predicate must be set");

            using var trans = modelContextBuilder.BuildDeferred();
            var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, trans);
            var readLayerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);

            var user = await currentUserService.GetCurrentUser(trans);
            if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

            if (!(await ciModel.CIIDExists(relation.FromCIID, trans)))
                return BadRequest($"CI with ID \"{relation.FromCIID}\" does not exist");
            if (!(await ciModel.CIIDExists(relation.ToCIID, trans)))
                return BadRequest($"CI with ID \"{relation.ToCIID}\" does not exist");

            var timeThreshold = TimeThreshold.BuildLatest();
            var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

            var (created, changed) = await relationModel.InsertRelation(relation.FromCIID, relation.ToCIID, relation.Predicate, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);

            // we fetch the just created relation again, but merged
            var r = await relationModel.GetMergedRelation(created.FromCIID, created.ToCIID, created.PredicateID, readLayerset, trans, timeThreshold);
            if (r == null)
                return BadRequest("Could not find relation");

            trans.Commit();

            return Created(Model2DTO(r));
        }

        [EnableQuery]
        public async Task<IActionResult> Delete([FromODataUri] Guid keyFromCIID, [FromODataUri] Guid keyToCIID, [FromODataUri] string keyPredicate, [FromRoute] string context)
        {
            try
            {
                using var trans = modelContextBuilder.BuildDeferred();
                var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, trans);
                var user = await currentUserService.GetCurrentUser(trans);
                if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                    return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

                var timeThreshold = TimeThreshold.BuildLatest();
                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);
                var (removed, changed) = await relationModel.RemoveRelation(keyFromCIID, keyToCIID, keyPredicate, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return NoContent();
        }

    }
}
