using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model.Config;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.OData
{
    public class TraitEntityController : ODataController
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly ODataAPIContextModel oDataAPIContextModel;
        private readonly ILogger<TraitEntityController> logger;

        public TraitEntityController(ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder, IMetaConfigurationModel metaConfigurationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel,
            ODataAPIContextModel oDataAPIContextModel, ILogger<TraitEntityController> logger)
        {
            this.traitsProvider = traitsProvider;
            this.modelContextBuilder = modelContextBuilder;
            this.metaConfigurationModel = metaConfigurationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.oDataAPIContextModel = oDataAPIContextModel;
            this.logger = logger;
        }

        public async Task<ActionResult<EdmEntityObjectCollection>> Get(string context, string entityset)
        {
            // Get Edm type from request.
            ODataPath path = Request.ODataFeature().Path;
            IEdmType edmType = path.Last().EdmType;
            Contract.Assert(edmType.TypeKind == EdmTypeKind.Collection);

            var model = Request.GetModel();

            var collectionType = edmType as IEdmCollectionType;
            var entityType = collectionType?.ElementType.Definition as IEdmEntityType;
            if (entityType == null)
                return BadRequest();

            SetSelectExpandClauseOnODataFeature(path, entityType);

            using var trans = modelContextBuilder.BuildImmediate();
            var timeThreshold = TimeThreshold.BuildLatest();

            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, metaConfigurationModel, context, trans, timeThreshold);

            var traitID = entityType.Name;
            var trait = await traitsProvider.GetActiveTrait(traitID, trans, timeThreshold);
            if (trait == null)
                return BadRequest();

            var traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel);

            try
            {
                ODataQueryContext queryContext = new ODataQueryContext(model, entityType, path);
                ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, Request);

                if (queryOptions.Filter != null)
                {
                    throw new Exception("We don't support filtering");
                } 
                else if (queryOptions.OrderBy != null)
                {
                    throw new Exception("We don't support ordering");
                }

                // TODO: use dataloader
                // TODO: integrate query options, if possible?
                var ets = await traitEntityModel.GetByCIID(new AllCIIDsSelection(), layerset, trans, timeThreshold);

                var e = ConvertEffectiveTraits2EdmEntities(ets.Select(kv => (ciid: kv.Key, et: kv.Value)), entityType, trait);

                return Ok(new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType), e.ToList()));
            } catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<ActionResult<IEdmEntityObject>> Get(string context, string entityset, Guid key)
        {
            // Get entity type from path.
            ODataPath path = Request.ODataFeature().Path;
            var entityType = path.Last().EdmType as IEdmEntityType;
            if (entityType == null)
                return BadRequest();

            //Set the SelectExpandClause on OdataFeature to include navigation property set in the $expand
            SetSelectExpandClauseOnODataFeature(path, entityType);

            var timeThreshold = TimeThreshold.BuildLatest();
            using var trans = modelContextBuilder.BuildImmediate();

            var traitID = entityType.Name;
            var trait = await traitsProvider.GetActiveTrait(traitID, trans, timeThreshold);
            if (trait == null)
                return BadRequest();

            var traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel);

            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, metaConfigurationModel, context, trans, timeThreshold);

            // TODO: use dataloader
            var ets = await traitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(key), layerset, trans, timeThreshold);
            var e = ConvertEffectiveTraits2EdmEntities(ets.Select(kv => (kv.Key, kv.Value)), entityType, trait);

            return Ok(e.FirstOrDefault());
        }

        public async Task<ActionResult<EdmEntityObjectCollection>> GetNavigation(string context, string entityset, Guid key, string navigation)
        {
            ODataPath path = Request.ODataFeature().Path;

            var sourceSegment = path.FirstOrDefault(s => s is EntitySetSegment) as EntitySetSegment;
            var sourceCollectionType = (sourceSegment?.EdmType as IEdmCollectionType);
            var sourceEntityType = sourceCollectionType?.ElementType.Definition as IEdmEntityType;

            IEdmEntityType? targetEntityType;
            if (path.Last() is EntitySetSegment targetEntitySegment) {
                targetEntityType = (targetEntitySegment?.EdmType as IEdmCollectionType)?.ElementType.Definition as IEdmEntityType;
            } 
            else if (path.Last() is NavigationPropertySegment targetNavigationSegment)
            {
                targetEntityType = targetNavigationSegment.NavigationProperty.ToEntityType();
            } else
            {
                return BadRequest();
            }

            if (sourceEntityType == null)
                return BadRequest();
            if (targetEntityType == null)
                return BadRequest();

            //Set the SelectExpandClause on OdataFeature to include navigation property set in the $expand
            SetSelectExpandClauseOnODataFeature(path, sourceEntityType);

            var timeThreshold = TimeThreshold.BuildLatest();
            using var trans = modelContextBuilder.BuildImmediate();

            var baseTraitID = sourceEntityType.Name;
            ITrait? baseTrait = await traitsProvider.GetActiveTrait(baseTraitID, trans, timeThreshold);
            if (baseTrait == null)
                return BadRequest();

            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, metaConfigurationModel, context, trans, timeThreshold);

            // split navigation into trait relation and other trait ID 
            var tmp = navigation.Split("_as_");
            var baseTraitRelationIdentifier = tmp[0];
            var otherTraitID = tmp[1];

            ITrait? otherTrait = await traitsProvider.GetActiveTrait(otherTraitID, trans, timeThreshold);
            if (otherTrait == null)
                return BadRequest();

            var traitRelation = baseTrait.OptionalRelations.FirstOrDefault(tr => tr.Identifier == baseTraitRelationIdentifier);
            if (traitRelation == null)
                return BadRequest();

            // TODO: use dataloader and batching
            var baseTraitEntityModel = new TraitEntityModel(baseTrait, effectiveTraitModel, ciModel, attributeModel, relationModel);
            var ets = await baseTraitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(key), layerset, trans, timeThreshold);
            var et = ets.FirstOrDefault().Value;

            if (et == null)
                return BadRequest();

            var otherCIIDs = (traitRelation.RelationTemplate.DirectionForward) ? et.OutgoingTraitRelations[traitRelation.Identifier].Select(r => r.Relation.ToCIID) : et.IncomingTraitRelations[traitRelation.Identifier].Select(r => r.Relation.FromCIID);

            var otherTraitEntityModel = new TraitEntityModel(otherTrait, effectiveTraitModel, ciModel, attributeModel, relationModel);

            // TODO: use dataloader
            var otherEts = await otherTraitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(otherCIIDs.ToHashSet()), layerset, trans, timeThreshold);

            var e = ConvertEffectiveTraits2EdmEntities(otherEts.Select(kv => (ciid: kv.Key, et: kv.Value)), targetEntityType, otherTrait);

            var targetCollectionTypeRef = new EdmCollectionTypeReference(new EdmCollectionType(new EdmEntityTypeReference(targetEntityType, true)));
            return Ok(new EdmEntityObjectCollection(targetCollectionTypeRef, e.ToList()));
        }

        private IEnumerable<IEdmEntityObject> ConvertEffectiveTraits2EdmEntities(IEnumerable<(Guid ciid, EffectiveTrait et)> ets, IEdmEntityType entityType, ITrait trait)
        {
            var e = ets.Select(tuple => {
                var (ciid, et) = tuple;
                try
                {
                    EdmEntityObject entity = new EdmEntityObject(entityType);
                    entity.TrySetPropertyValue("ciid", ciid);
                    foreach(var requiredAttribute in trait.RequiredAttributes)
                    {
                        if (!et.TraitAttributes.TryGetValue(requiredAttribute.Identifier, out var attribute))
                        {
                            throw new Exception($"Expected effective trait \"{trait.ID}\" of ciid \"{ciid}\" to contain required attribute \"{requiredAttribute.Identifier}\"");
                        }
                        entity.TrySetPropertyValue(requiredAttribute.Identifier, EdmModelHelper.AttributeValue2EdmValue(attribute.Attribute.Value));
                    }
                    foreach (var optionalAttribute in trait.OptionalAttributes)
                    {
                        if (et.TraitAttributes.TryGetValue(optionalAttribute.Identifier, out var attribute))
                        {
                            entity.TrySetPropertyValue(optionalAttribute.Identifier, EdmModelHelper.AttributeValue2EdmValue(attribute.Attribute.Value));
                        } else
                        {
                            entity.TrySetPropertyValue(optionalAttribute.Identifier, null);
                        }
                    }

                    // TODO:, when client wants expanded properties, add them through relations
                    // example https://localhost:44378/api/odata/testcontext/insight_discovery.hosts?$expand=has_patch_installed_as_insight_discovery.patch
                    // get queryOptions, look into Expand options and fetch correct related entities, add here

                    return entity as IEdmEntityObject;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Could not convert effective trait to Edm entity");
                    return null;
                }
            }).Where(e => e != null).Cast<IEdmEntityObject>();

            return e;
        }

        /// <summary>
        /// Set the <see cref="SelectExpandClause"/> on ODataFeature.
        /// Without this, the response does not contains navigation property included in $expand
        /// </summary>
        /// <param name="odataPath">OData Path from the Request</param>
        /// <param name="edmEntityType">Entity type on which the query is being performed</param>
        /// <returns></returns>
        private void SetSelectExpandClauseOnODataFeature(ODataPath odataPath, IEdmType edmEntityType)
        {
            IDictionary<string, string> options = new Dictionary<string, string>();
            foreach (var k in Request.Query.Keys)
            {
                options.Add(k, Request.Query[k]);
            }

            //At this point, we should have valid entity segment and entity type.
            //If there is invalid entity in the query, then OData routing should return 404 error before executing this api
            var segment = odataPath.FirstSegment as EntitySetSegment;
            IEdmNavigationSource? source = segment?.EntitySet;
            ODataQueryOptionParser parser = new(Request.GetModel(), edmEntityType, source, options);
            //Set the SelectExpand Clause on the ODataFeature otherwise  Odata formatter won't show the expand and select properties in the response.
            Request.ODataFeature().SelectExpandClause = parser.ParseSelectAndExpand();
        }
    }
}
