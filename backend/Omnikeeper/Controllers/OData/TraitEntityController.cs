using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using Omnikeeper.Model.Config;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.OData
{
    [ApiExplorerSettings(IgnoreApi = true)] // NOTE: swashbuckle has troubles creating unique operationIDs for this controller, so we skip it (for now)
    [Authorize(AuthenticationSchemes = "ODataBasicAuthentication")]
    public class TraitEntityController : ODataController
    {
        private readonly ITraitsHolder traitsHolder;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IChangesetModel changesetModel;
        private readonly ODataAPIContextModel oDataAPIContextModel;
        private readonly ILogger<TraitEntityController> logger;

        public TraitEntityController(ITraitsHolder traitsHolder, IModelContextBuilder modelContextBuilder, IMetaConfigurationModel metaConfigurationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel,
            ODataAPIContextModel oDataAPIContextModel, ILogger<TraitEntityController> logger)
        {
            this.traitsHolder = traitsHolder;
            this.modelContextBuilder = modelContextBuilder;
            this.metaConfigurationModel = metaConfigurationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.changesetModel = changesetModel;
            this.oDataAPIContextModel = oDataAPIContextModel;
            this.logger = logger;
        }

        public async Task<ActionResult<EdmEntityObjectCollection>> Get(string context, string entityset)
        {
            logger.LogDebug("GetAll - {context} - {entityset} - {query}", context, entityset, Request.QueryString);

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

            var traits = traitsHolder.GetTraits();

            var traitID = entityType.Name;
            if (!traits.TryGetValue(traitID, out var trait))
                return BadRequest();

            var traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);

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

                // TODO: implement top query option

                // TODO: use dataloader
                // TODO: integrate query options, if possible?
                var ets = await traitEntityModel.GetByCIID(AllCIIDsSelection.Instance, layerset, trans, timeThreshold);

                IDictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>> expandedETs = new Dictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>>();
                if (queryOptions.SelectExpand != null)
                    expandedETs = await CalculateExpandedETs(queryOptions.SelectExpand.SelectExpandClause, ets, trait, traits, layerset, trans, timeThreshold);

                var e = ConvertEffectiveTraits2EdmEntities(ets.Select(kv => (ciid: kv.Key, et: kv.Value)), entityType, trait, expandedETs);

                return Ok(new EdmEntityObjectCollection(new EdmCollectionTypeReference(collectionType), e.ToList()));
            } catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        private async Task<IDictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>>> CalculateExpandedETs(SelectExpandClause selectExpandClause, IDictionary<Guid, EffectiveTrait> baseETs, ITrait baseTrait, IDictionary<string, ITrait> traits, LayerSet layerset, IModelContext trans, TimeThreshold timeThreshold)
        {
            var expandedETs = new Dictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>>();
            foreach (var selectedItem in selectExpandClause.SelectedItems)
            {
                if (selectedItem is ExpandedNavigationSelectItem ei)
                {
                    var innerPath = ei.PathToNavigationProperty;
                    var lastInPath = innerPath.LastSegment;
                    var ns = ei.NavigationSource;
                    var innerEdmType = ns.EntityType();
                    Contract.Assert(innerEdmType.TypeKind == EdmTypeKind.Entity);

                    var innerTraitID = innerEdmType.Name;
                    if (!traits.TryGetValue(innerTraitID, out var innerTrait))
                        throw new Exception($"Could not find trait with ID \"{innerTraitID}\"");

                    // split navigation into trait relation and other trait ID 
                    var navigation = lastInPath.Identifier;
                    var tmp = navigation.Split("_as_");
                    var baseTraitRelationIdentifier = tmp[0];

                    var traitRelation = baseTrait.OptionalRelations.FirstOrDefault(tr => tr.Identifier == baseTraitRelationIdentifier);
                    if (traitRelation == null)
                        throw new Exception($"Could not find trait relation with identifier \"{baseTraitRelationIdentifier}\" in trait with ID \"{baseTrait.ID}\"");

                    var otherCIsDict = baseETs.ToDictionary(kv => kv.Key, kv => {
                        if (traitRelation.RelationTemplate.DirectionForward)
                            return kv.Value.OutgoingTraitRelations[traitRelation.Identifier].Select(mr => mr.Relation.ToCIID);
                        else
                            return kv.Value.IncomingTraitRelations[traitRelation.Identifier].Select(mr => mr.Relation.FromCIID);
                    });

                    var innerTraitEntityModel = new TraitEntityModel(innerTrait, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);
                    var innerEts = await innerTraitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(otherCIsDict.Values.SelectMany(e => e).ToHashSet()), layerset, trans, timeThreshold);

                    // nested expands
                    var innerExpandedETs = await CalculateExpandedETs(ei.SelectAndExpand, innerEts, innerTrait, traits, layerset, trans, timeThreshold);

                    // re-distribute innerETs to each baseET
                    var innerEDMEntities = otherCIsDict.ToDictionary(kv => kv.Key, kv =>
                    {
                        var innerET = kv.Value.Select(innerCIID => (innerCIID, innerEts[innerCIID]));

                        var innerInnerEDMEntities = ConvertEffectiveTraits2EdmEntities(innerET, innerEdmType, innerTrait, innerExpandedETs);
                        return innerInnerEDMEntities;
                    });

                    expandedETs.Add(navigation, innerEDMEntities);
                }
            }
            return expandedETs;
        }

        public async Task<ActionResult<IEdmEntityObject>> Get(string context, string entityset, Guid key)
        {
            logger.LogDebug("GetSingle - {context} - {entityset} - {key} - {query}", context, entityset, key, Request.QueryString);

            // Get entity type from path.
            ODataPath path = Request.ODataFeature().Path;
            var entityType = path.Last().EdmType as IEdmEntityType;
            if (entityType == null)
                return BadRequest();

            //Set the SelectExpandClause on OdataFeature to include navigation property set in the $expand
            SetSelectExpandClauseOnODataFeature(path, entityType);

            // TODO: support for expand?

            var timeThreshold = TimeThreshold.BuildLatest();
            using var trans = modelContextBuilder.BuildImmediate();

            var traits = traitsHolder.GetTraits();

            var traitID = entityType.Name;
            if (!traits.TryGetValue(traitID, out var trait))
                return BadRequest();

            var traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);

            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, metaConfigurationModel, context, trans, timeThreshold);

            // TODO: use dataloader
            var ets = await traitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(key), layerset, trans, timeThreshold);
            var e = ConvertEffectiveTraits2EdmEntities(ets.Select(kv => (kv.Key, kv.Value)), entityType, trait, ImmutableDictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>>.Empty);

            return Ok(e.FirstOrDefault());
        }

        public async Task<ActionResult<EdmEntityObjectCollection>> GetNavigation(string context, string entityset, Guid key, string navigation)
        {
            logger.LogDebug("GetNavigation - {context} - {entityset} - {key} - {navigation} - {query}", context, entityset, key, navigation, Request.QueryString);

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

            var traits = traitsHolder.GetTraits();

            var baseTraitID = sourceEntityType.Name;
            if (!traits.TryGetValue(baseTraitID, out var baseTrait))
                return BadRequest();

            var layerset = await ODataAPIContextService.GetReadLayersetFromContext(oDataAPIContextModel, metaConfigurationModel, context, trans, timeThreshold);

            // split navigation into trait relation and other trait ID 
            var tmp = navigation.Split("_as_");
            var baseTraitRelationIdentifier = tmp[0];
            var otherTraitID = tmp[1].Replace("__", ".");

            if (!traits.TryGetValue(otherTraitID, out var otherTrait))
                return BadRequest();

            var traitRelation = baseTrait.OptionalRelations.FirstOrDefault(tr => tr.Identifier == baseTraitRelationIdentifier);
            if (traitRelation == null)
                return BadRequest();

            // TODO: use dataloader
            // NOTE: we don't need to get the full entity, the correct relation is enough
            var rs = (traitRelation.RelationTemplate.DirectionForward) ?
                RelationSelectionFrom.Build(new HashSet<string>() { traitRelation.RelationTemplate.PredicateID }, key) :
                RelationSelectionTo.Build(new HashSet<string>() { traitRelation.RelationTemplate.PredicateID }, key);
            var relations = await relationModel.GetMergedRelations(rs, layerset, trans, timeThreshold, MaskHandlingForRetrievalApplyMasks.Instance, GeneratedDataHandlingInclude.Instance);
            if (relations == null)
                return BadRequest();
            var otherCIIDs = (traitRelation.RelationTemplate.DirectionForward) ?
                relations.Select(r => r.Relation.ToCIID).ToHashSet() :
                relations.Select(r => r.Relation.FromCIID).ToHashSet();

            var otherTraitEntityModel = new TraitEntityModel(otherTrait, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);

            // TODO: use dataloader
            var otherEts = await otherTraitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(otherCIIDs.ToHashSet()), layerset, trans, timeThreshold);

            var e = ConvertEffectiveTraits2EdmEntities(otherEts.Select(kv => (ciid: kv.Key, et: kv.Value)), targetEntityType, otherTrait, ImmutableDictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>>.Empty);

            var targetCollectionTypeRef = new EdmCollectionTypeReference(new EdmCollectionType(new EdmEntityTypeReference(targetEntityType, true)));
            return Ok(new EdmEntityObjectCollection(targetCollectionTypeRef, e.ToList()));
        }

        private IEnumerable<IEdmEntityObject> ConvertEffectiveTraits2EdmEntities(IEnumerable<(Guid ciid, EffectiveTrait et)> ets, IEdmEntityType entityType, ITrait trait, IDictionary<string, IDictionary<Guid, IEnumerable<IEdmEntityObject>>> expandedETs)
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

                    // NOTE:, when client wants expanded properties, add them through relations
                    // example https://localhost:44378/api/odata/testcontext/insight_discovery.hosts?$expand=has_patch_installed_as_insight_discovery.patch
                    foreach (var optionalRelation in trait.OptionalRelations)
                    {
                        foreach(var traitHint in optionalRelation.RelationTemplate.TraitHints)
                        {
                            var key = optionalRelation.Identifier + "_as_" + traitHint.Replace(".", "__");
                            if (expandedETs.TryGetValue(key, out var expandedET))
                            {
                                if (expandedET.TryGetValue(ciid, out var expanded))
                                    entity.TrySetPropertyValue(key, expanded);
                            }
                        }
                    }

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
