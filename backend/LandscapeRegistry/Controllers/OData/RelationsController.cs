using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers.OData
{
    public class RelationDTO
    {
        [Key]
        public Guid FromCIID { get; set; }
        [Key]
        public Guid ToCIID { get; set; }
        [Key]
        public string Predicate { get; set; }
    }

    //[Authorize]
    public class RelationsController : ODataController
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IChangesetModel changesetModel;
        private readonly IODataAPIContextModel oDataAPIContextModel;
        private readonly NpgsqlConnection conn;
        private readonly ICurrentUserService currentUserService;
        private readonly IRegistryAuthorizationService authorizationService;

        public RelationsController(IRelationModel relationModel, ICIModel ciModel, IChangesetModel changesetModel, IODataAPIContextModel oDataAPIContextModel,
            ICurrentUserService currentUserService, IRegistryAuthorizationService authorizationService, NpgsqlConnection conn)
        {
            this.relationModel = relationModel;
            this.ciModel = ciModel;
            this.changesetModel = changesetModel;
            this.oDataAPIContextModel = oDataAPIContextModel;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
            this.conn = conn;
        }

        private RelationDTO Model2DTO(MergedRelation r)
        {
            return new RelationDTO() { FromCIID = r.Relation.FromCIID, ToCIID = r.Relation.ToCIID, Predicate = r.Relation.PredicateID };
        }


        [EnableQuery]
        public async Task<RelationDTO> GetRelationDTO([FromODataUri, Required] Guid keyFromCIID, [FromODataUri, Required] Guid keyToCIID, [FromODataUri, Required] string keyPredicate, [FromRoute] string context)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, null);
            var r = await relationModel.GetMergedRelation(keyFromCIID, keyToCIID, keyPredicate, layerset, null, timeThreshold);
            return Model2DTO(r);
        }

        [EnableQuery]
        public async Task<IEnumerable<RelationDTO>> GetRelations([FromRoute] string context)
        {
            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, null);
            var relations = await relationModel.GetMergedRelations(new RelationSelectionAll(), layerset, null, TimeThreshold.BuildLatest());

            return relations.Select(r => Model2DTO(r));
        }

        [EnableQuery]
        public async Task<IActionResult> Post([FromBody] RelationDTO relation, [FromRoute] string context)
        {
            if (relation == null)
                return BadRequest($"Could not parse inserted relation");
            if (relation.FromCIID == null)
                return BadRequest($"Relation from CIID must be set");
            if (relation.ToCIID == null)
                return BadRequest($"Relation to CIID must be set");
            if (relation.Predicate == null || relation.Predicate == "")
                return BadRequest($"Relation Predicate must be set");

            var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, null);
            var readLayerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, null);

            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

            if (!(await ciModel.CIIDExists(relation.FromCIID, null)))
                return BadRequest($"CI with ID \"{relation.FromCIID}\" does not exist");
            if (!(await ciModel.CIIDExists(relation.ToCIID, null)))
                return BadRequest($"CI with ID \"{relation.ToCIID}\" does not exist");

            using var trans = conn.BeginTransaction();
            var timeThreshold = TimeThreshold.BuildLatest();

            var changesetProxy = ChangesetProxy.Build(user.InDatabase, timeThreshold.Time, changesetModel);

            var (created, changed) = await relationModel.InsertRelation(relation.FromCIID, relation.ToCIID, relation.Predicate, writeLayerID, changesetProxy, trans);

            // we fetch the just created relation again, but merged
            var r = await relationModel.GetMergedRelation(created.FromCIID, created.ToCIID, created.PredicateID, readLayerset, trans, timeThreshold);

            trans.Commit();

            return Created(Model2DTO(r));
        }

        [EnableQuery]
        public async Task<IActionResult> Delete([FromODataUri] Guid keyFromCIID, [FromODataUri] Guid keyToCIID, [FromODataUri] string keyPredicate, [FromRoute] string context)
        {
            var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, null);
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

            try
            {
                using var trans = conn.BeginTransaction();
                var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
                var (removed, changed) = await relationModel.RemoveRelation(keyFromCIID, keyToCIID, keyPredicate, writeLayerID, changesetProxy, trans);
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
