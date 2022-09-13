using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class GenericTraitEntityModel<T, ID> : GenericTraitEntityModel<T> where T : TraitEntity, new() where ID : notnull, IEquatable<ID>
    {
        private readonly GenericTraitEntityIDAttributeInfos<T, ID> idAttributeInfos;

        public GenericTraitEntityModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel)
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
            idAttributeInfos = GenericTraitEntityHelper.ExtractIDAttributeInfos<T, ID>();
        }

        public async Task<(T entity, Guid ciid)> GetSingleByDataID(ID id, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var idAttributeValues = idAttributeInfos.ExtractAttributeValuesFromID(id);
            var foundCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeInfos.GetIDAttributeNames().Zip(idAttributeValues).ToArray(), layerSet, trans, timeThreshold);

            if (foundCIIDs.IsEmpty())
                return default;

            // if the foundCIIDs contains any CIs that have the trait, we need to use this, not just the first CIID (which might NOT have the trait)
            var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(foundCIIDs, traitEntityModel, layerSet, trans, timeThreshold);

            if (bestMatchingET == null)
                return default;

            var entity = GenericTraitEntityHelper.EffectiveTrait2Object<T>(bestMatchingET, attributeFieldInfos, relationFieldInfos);

            return (entity, bestMatchingCIID);
        }

        public async Task<IDictionary<ID, T>> GetByDataID(ICIIDSelection ciSelection, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ets = (await traitEntityModel.GetByCIID(ciSelection, layerSet, trans, timeThreshold))
                .Values
                .OrderBy(et => et.CIID); // we order by CIID to stay consistent even when multiple CIs would match

            var ret = new Dictionary<ID, T>();
            foreach (var et in ets)
            {
                var dc = GenericTraitEntityHelper.EffectiveTrait2Object<T>(et, attributeFieldInfos, relationFieldInfos);
                var id = idAttributeInfos.ExtractIDFromEntity(dc);
                if (!ret.ContainsKey(id))
                {
                    ret[id] = dc;
                }
            }
            return ret;
        }

        /*
         * NOTE: this does not care whether or not the CI is actually a trait entity or not
         */
        // TODO: improve performance for tuple-id case
        private async Task<IDictionary<ID, Guid>> FindMatchingCIIDsFromIDs(string[] attributeNames, ID[] ids, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (ids.Length == 0)
                return new Dictionary<ID, Guid>();

            var cisWithIDAttributes = await attributeModel.GetMergedAttributes(AllCIIDsSelection.Instance, NamedAttributesSelection.Build(attributeNames.ToHashSet()), layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

            // invert the dicationary and get a (attributeName, attributeValue) -> list of ciid
            var lookup = cisWithIDAttributes
                .SelectMany(t => t.Value.Select(tt => (ciid: t.Key, attributeName: tt.Key, attributeValue: tt.Value.Attribute.Value)))
                .GroupBy(t => (t.attributeName, t.attributeValue), t => t.ciid)
                .ToDictionary(t => t.Key, t => t.ToHashSet());

            var ret = new Dictionary<ID, Guid>(ids.Length);
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];

                var idValues = idAttributeInfos.ExtractAttributeValuesFromID(id);

                var candidatesLists = idValues.Select((attributeValue, j) =>
                {
                    var attributeName = attributeNames[j];
                    if (lookup.TryGetValue((attributeName, attributeValue), out var candidates))
                        return candidates;
                    return (IEnumerable<Guid>)ImmutableList<Guid>.Empty;
                }).ToList();

                HashSet<Guid>? fitting;

                // check if any of the candidate lists is empty, if so, we don't even need to compare anything anymore
                if (candidatesLists.Any(cl => cl.IsEmpty()))
                {
                    fitting = new HashSet<Guid>();
                } else
                {
                    fitting = candidatesLists.Skip(1).Aggregate(new HashSet<Guid>(candidatesLists.First()), (h, e) => { h.IntersectWith(e); return h; });
                }

                if (fitting.Count == 1)
                    ret[id] = fitting.First();
                else if (fitting.Count > 1)
                {
                    ret[id] = fitting.OrderBy(t => t).FirstOrDefault(); // we order by GUID to stay consistent even when multiple CIs would match
                }
            }

            return ret;
        }

        /*
         * NOTE: unlike the regular insert, this does not do any checks if the updated entities actually fulfill the trait requirements 
         * and will be considered as this trait's entities going forward
         * NOTE: relevantCISelection is only an upper bound on the relevant CIs; only CIs in this selection that ALSO fulfill the trait are considered
         * NOTE: does not offer any special handling for naming CIs; if you need to write __name attributes, add the attribute to the trait
         */
        public async Task<bool> BulkReplace(ICIIDSelection relevantCISelection, IDictionary<ID, T> t, LayerSet layerSet, string writeLayer, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            if (t.IsEmpty() && relevantCISelection is NoCIIDsSelection)
                return false;

            var outdated = await GetByCIID(relevantCISelection, layerSet, trans, changesetProxy.TimeThreshold);
            // NOTE: we use Lookups instead of Dictionaries to support duplicate IDs at this level
            var outdatedCIIDLookup = outdated.ToLookup(kv => idAttributeInfos.ExtractIDFromEntity(kv.Value), kv => kv.Key);

            var outdatedIDs = outdated.Select(g => idAttributeInfos.ExtractIDFromEntity(g.Value)).ToHashSet();

            var IDsOfNotFoundEntities = t.Keys.Except(outdatedIDs).ToArray();

            // for new entities that do not have a fully matching entity that already exists, we try to find a CI where the ID attributes match
            var newCIDictionary = new Dictionary<ID, Guid>();
            var idMatchedCIDictionary = await FindMatchingCIIDsFromIDs(idAttributeInfos.GetIDAttributeNames(), IDsOfNotFoundEntities, layerSet, trans, changesetProxy.TimeThreshold);
            for (var i = 0; i < IDsOfNotFoundEntities.Length; i++)
            {
                var id = IDsOfNotFoundEntities[i];
                if (!idMatchedCIDictionary.ContainsKey(id))
                {
                    newCIDictionary[id] = Guid.NewGuid();
                }
            }

            // for entities that we really couldn't find a matching CI, create new CIs
            await ciModel.BulkCreateCIs(newCIDictionary.Select(kv => kv.Value), trans);

            var entities = t.Select(kv =>
            {
                var id = kv.Key;
                var ciidInOutdated = outdatedCIIDLookup[id];
                Guid ciid;
                if (!ciidInOutdated.IsEmpty())
                    ciid = ciidInOutdated.OrderBy(ciid => ciid).First(); // stay consistent
                else if (idMatchedCIDictionary.TryGetValue(id, out var outCiid))
                    ciid = outCiid;
                else
                    ciid = newCIDictionary[id];
                return (kv.Value, ciid);
            });

            var relevantCIIDs = newCIDictionary.Values.Union(idMatchedCIDictionary.Values).Union(outdated.Keys).ToImmutableHashSet();
            var attributeFragments = Entities2Fragments(entities);
            var (outgoingRelations, incomingRelations) = Entities2RelationTuples(entities);
            var changed = await traitEntityModel.BulkReplace(relevantCIIDs, attributeFragments, outgoingRelations, incomingRelations, layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);

            return changed;
        }

        public async Task<(T dc, bool changed, Guid ciid)> InsertOrUpdate(T t, LayerSet layerSet, string writeLayer, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            // NOTE: we do a CIID lookup based on the ID attributes and their values, but we DON'T require that the found CI must already be a trait entity
            var id = idAttributeInfos.ExtractIDFromEntity(t);
            var idAttributeValues = idAttributeInfos.ExtractAttributeValuesFromID(id);
            var foundCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeInfos.GetIDAttributeNames().Zip(idAttributeValues).ToArray(), layerSet, trans, changesetProxy.TimeThreshold);

            Guid ciid;
            if (foundCIIDs.IsEmpty())
                ciid = await ciModel.CreateCI(trans);
            else
            {
                // if the foundCIIDs contains any CIs that have the trait, we need to use this preferably, not just the first CIID (which might NOT have the trait)
                // only if there are no CIs that fulfill the trait, we can use the first one ordered by CIID only
                var (bestMatchingCIID, _) = await TraitEntityHelper.GetSingleBestMatchingCI(foundCIIDs, traitEntityModel, layerSet, trans, changesetProxy.TimeThreshold);
                ciid = bestMatchingCIID;
            }

            var tuples = new (T t, Guid ciid)[] { (t, ciid) };
            var attributeFragments = Entities2Fragments(tuples);
            var (outgoingRelations, incomingRelations) = Entities2RelationTuples(tuples);
            string? ciName = null;
            // TODO: we should specify the actually relevant trait relations (through their predicateIDs, not ALL relations)
            var (et, changed) = await traitEntityModel.InsertOrUpdate(ciid, attributeFragments, outgoingRelations, incomingRelations, null, null, ciName, layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);

            var dc = GenericTraitEntityHelper.EffectiveTrait2Object<T>(et, attributeFieldInfos, relationFieldInfos);

            return (dc, changed, ciid);
        }

        public async Task<bool> TryToDelete(ID id, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            // TODO: we should actually not fetch a single, but instead ALL that match that ID and delete them
            // only then, the end result is that no CI matching the trait with that ID exists anymore, otherwise it's not guaranteed and deleting fails because there's still a matching ci afterwards
            var t = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);
            if (t == default)
            {
                return false; // no dc with this ID exists
            }

            return await traitEntityModel.TryToDelete(t.ciid, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);
        }
    }

    public class GenericTraitEntityModel<T> where T : TraitEntity, new()
    {
        protected readonly ICIModel ciModel;
        protected readonly IAttributeModel attributeModel;
        protected readonly IEnumerable<TraitAttributeFieldInfo> attributeFieldInfos;
        protected readonly IEnumerable<TraitRelationFieldInfo> relationFieldInfos;

        protected readonly TraitEntityModel traitEntityModel;

        public GenericTraitEntityModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel)
        {
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            var trait = RecursiveTraitService.FlattenSingleRecursiveTrait(GenericTraitEntityHelper.Class2RecursiveTrait<T>());

            (_, attributeFieldInfos, relationFieldInfos) = GenericTraitEntityHelper.ExtractFieldInfos<T>();

            traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);
        }

        public async Task<T?> GetSingleByCIID(Guid ciid, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var et = await traitEntityModel.GetSingleByCIID(ciid, layerSet, trans, timeThreshold);
            if (et == null)
                return null;
            var dc = GenericTraitEntityHelper.EffectiveTrait2Object<T>(et, attributeFieldInfos, relationFieldInfos);
            return dc;
        }

        public async Task<IDictionary<Guid, T>> GetByCIID(ICIIDSelection ciSelection, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ets = await traitEntityModel.GetByCIID(ciSelection, layerSet, trans, timeThreshold);
            return ets.ToDictionary(kv => kv.Key, kv => GenericTraitEntityHelper.EffectiveTrait2Object<T>(kv.Value, attributeFieldInfos, relationFieldInfos));
        }

        // returns the latest relevant changeset that affects/contributes to any of the trait entities (filtered by ciSelection) at that time
        public async Task<Changeset?> GetLatestRelevantChangesetOverall(ICIIDSelection ciSelection, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await traitEntityModel.GetLatestRelevantChangesetOverall(ciSelection, layerSet, trans, timeThreshold);
        }

        public async Task<bool> TryToDelete(Guid ciid, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            return await traitEntityModel.TryToDelete(ciid, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);
        }

        protected IList<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> Entities2Fragments(IEnumerable<(T t, Guid ciid)> entities)
        {
            var fragments = new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>();
            foreach (var taFieldInfo in attributeFieldInfos)
            {
                foreach (var (t, ciid) in entities)
                {
                    var entityValue = taFieldInfo.Accessor.Get(t);
                    var attributeName = taFieldInfo.TraitAttributeAttribute.aName;

                    if (entityValue != null)
                    {
                        IAttributeValue value;
                        if (taFieldInfo.AttributeValueType == AttributeValueType.JSON && taFieldInfo.JsonSerializer != null)
                        { // json with serializer
                            value = taFieldInfo.JsonSerializer.SerializeToAttributeValue(entityValue, taFieldInfo.IsArray);
                        }
                        else
                        { // general attribute
                            value = AttributeValueHelper.BuildFromTypeAndObject(taFieldInfo.AttributeValueType, entityValue);
                        }
                        fragments.Add(new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, attributeName, value));
                    }
                    else
                    {
                        if (!taFieldInfo.TraitAttributeAttribute.optional)
                        {
                            throw new Exception(); // TODO
                        }
                        else
                        {
                            // this is an optional attribute, and it is not set, so we'll try to remove the attribute, which happens implicitly through the bulk operation
                        }
                    }
                }
            }
            return fragments;
        }
        protected (IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations) Entities2RelationTuples(IEnumerable<(T t, Guid ciid)> entities)
        {
            var outgoingRelations = new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>();
            var incomingRelations = new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>();
            if (!relationFieldInfos.IsEmpty())
            {
                foreach (var (t, ciid) in entities)
                {
                    foreach (var trFieldInfo in relationFieldInfos)
                    {
                        var entityValue = trFieldInfo.Accessor.Get(t);

                        if (entityValue != null)
                        {
                            var predicateID = trFieldInfo.TraitRelationAttribute.predicateID;
                            var outgoing = trFieldInfo.TraitRelationAttribute.directionForward;

                            if (entityValue is Guid[] otherCIIDs)
                            {
                                if (outgoing)
                                    outgoingRelations.Add((ciid, predicateID, otherCIIDs));
                                else
                                    incomingRelations.Add((ciid, predicateID, otherCIIDs));
                            }
                            else if (entityValue is Guid otherCIID)
                            {
                                if (outgoing)
                                    outgoingRelations.Add((ciid, predicateID, new Guid[] { otherCIID }));
                                else
                                    incomingRelations.Add((ciid, predicateID, new Guid[] { otherCIID }));
                            }
                            else throw new Exception(); // invalid type
                        }
                        else
                        {
                            // relations are optional by design, continue if not found
                        }
                    }
                }
            }

            return (outgoingRelations, incomingRelations);
        }
    }

    public class TraitAttributeFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public readonly FieldAccessor Accessor;
        public readonly TraitAttributeAttribute TraitAttributeAttribute;
        public readonly AttributeValueType AttributeValueType;
        public readonly bool IsArray;
        public readonly bool IsID;
        public readonly IAttributeJSONSerializer? JsonSerializer;

        public TraitAttributeFieldInfo(FieldInfo fieldInfo, Type entityType, TraitAttributeAttribute traitAttributeAttribute, AttributeValueType attributeValueType, bool isArray, bool isID, IAttributeJSONSerializer? jsonSerializer)
        {
            FieldInfo = fieldInfo;
            Accessor = new FieldAccessor(fieldInfo, entityType);
            TraitAttributeAttribute = traitAttributeAttribute;
            AttributeValueType = attributeValueType;
            IsArray = isArray;
            IsID = isID;
            JsonSerializer = jsonSerializer;
        }
    }

    public class TraitRelationFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public readonly FieldAccessor Accessor;
        public readonly TraitRelationAttribute TraitRelationAttribute;

        public TraitRelationFieldInfo(FieldInfo fieldInfo, Type entityType, TraitRelationAttribute traitRelationAttribute)
        {
            FieldInfo = fieldInfo;
            Accessor = new FieldAccessor(fieldInfo, entityType);
            TraitRelationAttribute = traitRelationAttribute;
        }
    }
}
