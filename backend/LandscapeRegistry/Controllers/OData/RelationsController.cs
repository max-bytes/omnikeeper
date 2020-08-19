using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using LandscapeRegistry.Service;

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

    [Authorize]
    public class RelationsController : ODataController
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IChangesetModel changesetModel;
        private readonly NpgsqlConnection conn;
        private readonly ICurrentUserService currentUserService;
        private readonly IRegistryAuthorizationService authorizationService;

        public RelationsController(IRelationModel relationModel, ICIModel ciModel, IChangesetModel changesetModel, ICurrentUserService currentUserService, IRegistryAuthorizationService authorizationService, NpgsqlConnection conn)
        {
            this.relationModel = relationModel;
            this.ciModel = ciModel;
            this.changesetModel = changesetModel;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
            this.conn = conn;
        }

        private RelationDTO Model2DTO(Relation a)
        {
            return new RelationDTO() { FromCIID = a.FromCIID, ToCIID = a.ToCIID, Predicate = a.PredicateID };
        }

        [EnableQuery]
        public async Task<RelationDTO> GetRelationDTO([FromODataUri, Required]Guid keyFromCIID, [FromODataUri, Required]Guid keyToCIID, [FromODataUri, Required]string keyPredicate, [FromRoute]int layerID)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var r = await relationModel.GetRelation(keyFromCIID, keyToCIID, keyPredicate, layerID, null, timeThreshold);
            return Model2DTO(r);
        }

        [EnableQuery]
        public async Task<IEnumerable<RelationDTO>> GetRelations([FromRoute]int layerID)
        {
            var relations = await relationModel.GetRelations(new RelationSelectionAll(), layerID, null, TimeThreshold.BuildLatest());

            return relations.Select(r => Model2DTO(r));
        }


        [EnableQuery]
        public async Task<IActionResult> Post([FromBody] RelationDTO relation, [FromRoute]int layerID)
        {
            if (relation == null)
                return BadRequest($"Could not parse inserted relation");
            if (relation.FromCIID == null)
                return BadRequest($"Relation from CIID must be set");
            if (relation.ToCIID == null)
                return BadRequest($"Relation to CIID must be set");
            if (relation.Predicate == null || relation.Predicate == "")
                return BadRequest($"Relation Predicate must be set");

            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");

            if (!(await ciModel.CIIDExists(relation.FromCIID, null)))
                return BadRequest($"CI with ID \"{relation.FromCIID}\" does not exist");
            if (!(await ciModel.CIIDExists(relation.ToCIID, null)))
                return BadRequest($"CI with ID \"{relation.ToCIID}\" does not exist");

            using var trans = conn.BeginTransaction();
            var timeThreshold = TimeThreshold.BuildLatest();

            var changesetProxy = ChangesetProxy.Build(user.InDatabase, timeThreshold.Time, changesetModel);

            var created = await relationModel.InsertRelation(relation.FromCIID, relation.ToCIID, relation.Predicate, layerID, changesetProxy, trans);

            trans.Commit();

            return Created(Model2DTO(created));
        }

        [EnableQuery]
        public async Task<IActionResult> Delete([FromODataUri]Guid keyFromCIID, [FromODataUri]Guid keyToCIID, [FromODataUri]string keyPredicate, [FromRoute]int layerID)
        {
            var user = await currentUserService.GetCurrentUser(null);
            if (!authorizationService.CanUserWriteToLayer(user, layerID))
                return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {layerID}");

            using var trans = conn.BeginTransaction();
            var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            var removed = await relationModel.RemoveRelation(keyFromCIID, keyToCIID, keyPredicate, layerID, changesetProxy, trans);

            if (removed == null)
                return NotFound();

            trans.Commit();

            return NoContent();
        }

    }
}
