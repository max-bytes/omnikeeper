//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.OData.Formatter;
//using Microsoft.AspNetCore.OData.Query;
//using Microsoft.AspNetCore.OData.Routing.Controllers;
//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Entity.DataOrigin;
//using Omnikeeper.Base.Model;
//using Omnikeeper.Base.Service;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Base.Utils.ModelContext;
//using Omnikeeper.Service;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Omnikeeper.Controllers.OData
//{
//    public class RelationDTO
//    {
//        public RelationDTO(Guid fromCIID, Guid toCIID, string predicate)
//        {
//            FromCIID = fromCIID;
//            ToCIID = toCIID;
//            Predicate = predicate;
//        }

//        [Key]
//        public Guid FromCIID { get; set; }
//        [Key]
//        public Guid ToCIID { get; set; }
//        [Key]
//        public string Predicate { get; set; }
//    }

//    // TODO: authentication
//    // TODO: ci based authorization
//    // TODO: layer based authorization
//    //[Authorize]
//    public class RelationsController : ODataController
//    {
//        private readonly IRelationModel relationModel;
//        private readonly ICIModel ciModel;
//        private readonly IChangesetModel changesetModel;
//        private readonly IODataAPIContextModel oDataAPIContextModel;
//        private readonly IModelContextBuilder modelContextBuilder;
//        private readonly ICurrentUserAccessor currentUserService;
//        private readonly ILayerBasedAuthorizationService authorizationService;

//        public RelationsController(IRelationModel relationModel, ICIModel ciModel, IChangesetModel changesetModel, IODataAPIContextModel oDataAPIContextModel,
//            ICurrentUserAccessor currentUserService, ILayerBasedAuthorizationService authorizationService, IModelContextBuilder modelContextBuilder)
//        {
//            this.relationModel = relationModel;
//            this.ciModel = ciModel;
//            this.changesetModel = changesetModel;
//            this.oDataAPIContextModel = oDataAPIContextModel;
//            this.currentUserService = currentUserService;
//            this.authorizationService = authorizationService;
//            this.modelContextBuilder = modelContextBuilder;
//        }

//        private RelationDTO Model2DTO(MergedRelation r)
//        {
//            return new RelationDTO(r.Relation.FromCIID, r.Relation.ToCIID, r.Relation.PredicateID);
//        }

//        [EnableQuery]
//        public async Task<IEnumerable<RelationDTO>> GetRelations([FromRoute] string context)
//        {
//            var trans = modelContextBuilder.BuildImmediate();
//            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, context, trans);
//            var relations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layerset, trans, TimeThreshold.BuildLatest(), MaskHandlingForRetrievalGetMasks.Instance, GeneratedDataHandlingInclude.Instance);

//            return relations.Select(r => Model2DTO(r));
//        }

//        [EnableQuery]
//        public async Task<IActionResult> Delete([FromODataUri] Guid keyFromCIID, [FromODataUri] Guid keyToCIID, [FromODataUri] string keyPredicate, [FromRoute] string context)
//        {
//            try
//            {
//                using var trans = modelContextBuilder.BuildDeferred();
//                var writeLayerID = await ODataAPIContextService.GetWriteLayerIDFromContext(oDataAPIContextModel, context, trans);
//                var user = await currentUserService.GetCurrentUser(trans);
//                if (!authorizationService.CanUserWriteToLayer(user, writeLayerID))
//                    return Forbid($"User \"{user.Username}\" does not have permission to write to layer ID {writeLayerID}");

//                var timeThreshold = TimeThreshold.BuildLatest();
//                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);
//                var changed = await relationModel.RemoveRelation(keyFromCIID, keyToCIID, keyPredicate, writeLayerID, changesetProxy, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
//                trans.Commit();
//            }
//            catch (Exception)
//            {
//                return BadRequest();
//            }

//            return NoContent();
//        }

//    }
//}
