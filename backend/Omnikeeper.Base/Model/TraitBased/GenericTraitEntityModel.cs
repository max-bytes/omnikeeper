using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class GenericTraitEntityModel<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        protected readonly ICIModel ciModel;
        protected readonly IAttributeModel attributeModel;
        protected readonly IRelationModel relationModel;
        private readonly GenericTrait trait;
        private readonly HashSet<string> relevantAttributesForTrait;
        private readonly IEnumerable<TraitAttributeFieldInfo> attributeFieldInfos;
        private readonly IEnumerable<TraitRelationFieldInfo> relationFieldInfos;

        private readonly IIDAttributeInfos<T, ID> idAttributeInfos;

        private static readonly MyJSONSerializer<object> DefaultSerializer = new MyJSONSerializer<object>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });

        public GenericTraitEntityModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel)
        {
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;

            trait = RecursiveTraitService.FlattenSingleRecursiveTrait(TraitEntityHelper.Class2RecursiveTrait<T>());
            relevantAttributesForTrait = trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Concat(trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet();

            (_, attributeFieldInfos, relationFieldInfos) = TraitEntityHelper.ExtractFieldInfos<T>();

            idAttributeInfos = TraitEntityHelper.ExtractIDAttributeInfos<T, ID>();
        }

        private async Task<(T entity, Guid ciid)> GetSingleByCIID(Guid ciid, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ci = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(ciid), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold)).FirstOrDefault();
            if (ci == null) return default;
            var ciWithTrait = await effectiveTraitModel.GetEffectiveTraitForCI(ci, trait, layerSet, trans, timeThreshold);
            if (ciWithTrait == null) return default;
            var dc = TraitEntityHelper.EffectiveTrait2Object<T>(ciWithTrait, DefaultSerializer);
            return (dc, ciid);
        }

        private async Task<IDictionary<Guid, T>> GetAllByCIID(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold);
            var cisWithTrait = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);
            return cisWithTrait.ToDictionary(kv => kv.Key, kv => TraitEntityHelper.EffectiveTrait2Object<T>(kv.Value, DefaultSerializer));
        }

        public async Task<(T entity, Guid ciid)> GetSingleByDataID(ID id, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cisWithIDAttribute = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), idAttributeInfos.GetAttributeSelectionForID(), layerSet, trans, timeThreshold);
            var foundCIID = idAttributeInfos.FilterCIAttributesWithMatchingID(id, cisWithIDAttribute);

            if (foundCIID == default)
            { // no fitting entity found
                return default;
            }

            var ret = await GetSingleByCIID(foundCIID, layerSet, trans, timeThreshold);
            return ret;
        }

        public async Task<IDictionary<ID, T>> GetAllByDataID(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold);
            return await GetAllByDataID(cis, layerSet, trans, timeThreshold);
        }

        public async Task<IDictionary<ID, T>> GetAllByDataID(IEnumerable<MergedCI> withinCIs, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cisWithTrait = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, withinCIs, layerSet, trans, timeThreshold);
            var all = new List<(T entity, Guid ciid)>();
            foreach (var (ciid, et) in cisWithTrait.Select(kv => (kv.Key, kv.Value)))
            {
                var dc = TraitEntityHelper.EffectiveTrait2Object<T>(et, DefaultSerializer);
                all.Add((dc, ciid));
            }
            all.OrderBy(dc => dc.ciid); // we order by GUID to stay consistent even when multiple CIs would match

            var ret = new Dictionary<ID, T>();
            foreach (var dc in all)
            {
                var id = idAttributeInfos.ExtractIDFromEntity(dc.entity);
                if (!ret.ContainsKey(id))
                {
                    ret[id] = dc.entity;
                }
            }
            return ret;
        }

        /*
         * NOTE: unlike the regular insert, this does not do any checks if the updated entities actually fulfill the trait requirements 
         * and will be considered as this trait's entities going forward
         */
        public async Task<bool> BulkReplace(IDictionary<ID, T> t, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (t.IsEmpty())
                return false;

            var outdated = await GetAllByCIID(layerSet, trans, changesetProxy.TimeThreshold);
            // NOTE: we use Lookups instead of Dictionaries to support duplicate IDs at this level
            var outdatedCIIDLookup = outdated.ToLookup(kv => idAttributeInfos.ExtractIDFromEntity(kv.Value), kv => kv.Key);

            var outdatedIDs = outdated.Select(g => idAttributeInfos.ExtractIDFromEntity(g.Value)).ToHashSet();

            var IDsOfNewEntities = t.Keys.Except(outdatedIDs);

            var newCIIDDictionary = IDsOfNewEntities.ToDictionary(id => id, id => Guid.NewGuid());
            await ciModel.BulkCreateCIs(newCIIDDictionary.Select(kv => kv.Value), trans);

            var entities = t.Select(kv =>
            {
                var id = kv.Key;
                var ciidInOutdated = outdatedCIIDLookup[id];
                Guid ciid;
                if (!ciidInOutdated.IsEmpty())
                    ciid = ciidInOutdated.OrderBy(ciid => ciid).First(); // stay consistent
                else
                    ciid = newCIIDDictionary[id];
                return (kv.Value, ciid);
            });

            var relevantCIIDs = newCIIDDictionary.Values.Union(outdated.Keys).ToHashSet();
            var changed = await WriteAttributes(entities, relevantCIIDs, writeLayer, dataOrigin, changesetProxy, trans);

            if (!relationFieldInfos.IsEmpty())
            {
                var relevantOutgoingRelations = relationFieldInfos.Where(rfi => rfi.TraitRelationAttribute.directionForward).SelectMany(rfi => relevantCIIDs.Select(ciid => (ciid, rfi.TraitRelationAttribute.predicateID))).ToHashSet();
                var relevantIncomingRelations = relationFieldInfos.Where(rfi => !rfi.TraitRelationAttribute.directionForward).SelectMany(rfi => relevantCIIDs.Select(ciid => (ciid, rfi.TraitRelationAttribute.predicateID))).ToHashSet();
                var tmpChanged = await WriteRelations(entities, relevantOutgoingRelations, relevantIncomingRelations, writeLayer, dataOrigin, changesetProxy, trans);
                changed = changed || tmpChanged;
            }

            return changed;
        }

        public async Task<(T dc, bool changed)> InsertOrUpdate(T t, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var id = idAttributeInfos.ExtractIDFromEntity(t);

            var current = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);

            var ciid = (current != default) ? current.ciid : await ciModel.CreateCI(trans);

            var tuples = new (T t, Guid ciid)[] { (t, ciid) };

            var relevantCIs = new HashSet<Guid>() { ciid };
            var changed = await WriteAttributes(tuples, relevantCIs, writeLayer, dataOrigin, changesetProxy, trans);

            if (!relationFieldInfos.IsEmpty())
            {
                var relevantOutgoingRelations = relationFieldInfos.Where(rfi => rfi.TraitRelationAttribute.directionForward).Select(rfi => (ciid, rfi.TraitRelationAttribute.predicateID)).ToHashSet();
                var relevantIncomingRelations = relationFieldInfos.Where(rfi => !rfi.TraitRelationAttribute.directionForward).Select(rfi => (ciid, rfi.TraitRelationAttribute.predicateID)).ToHashSet();
                var tmpChanged = await WriteRelations(tuples, relevantOutgoingRelations, relevantIncomingRelations, writeLayer, dataOrigin, changesetProxy, trans);
                changed = changed || tmpChanged;
            }

            var dc = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == default)
                throw new Exception("DC does not conform to trait requirements");
            return (dc.entity, changed);
        }

        private async Task<bool> WriteAttributes(IEnumerable<(T t, Guid ciid)> entities, ISet<Guid> relevantCIs, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var fragments = new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>();
            foreach (var taFieldInfo in attributeFieldInfos)
            {
                foreach (var (t, ciid) in entities)
                {
                    var entityValue = taFieldInfo.FieldInfo.GetValue(t);
                    var attributeName = taFieldInfo.TraitAttributeAttribute.aName;

                    if (entityValue != null)
                    {
                        if (taFieldInfo.AttributeValueType == AttributeValueType.JSON && taFieldInfo.TraitAttributeAttribute.isJSONSerialized)
                        {
                            // serialize before storing as attribute
                            if (taFieldInfo.IsArray)
                            {
                                var a = (object[])entityValue;
                                var serialized = new JObject[a.Length];
                                for (int i = 0; i < a.Length; i++)
                                {
                                    var e = DefaultSerializer.SerializeToJObject(a[i]);
                                    serialized[i] = e;
                                }
                                entityValue = serialized;
                            }
                            else
                            {
                                entityValue = DefaultSerializer.SerializeToJObject(entityValue);
                            }
                        }

                        var value = AttributeValueBuilder.BuildFromTypeAndObject(taFieldInfo.AttributeValueType, entityValue);

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

            var relevantAttributeNames = attributeFieldInfos.Select(afi => afi.TraitAttributeAttribute.aName).ToHashSet();
            var changed = await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataCIAndAttributeNameScope(writeLayer, fragments, relevantCIs, relevantAttributeNames), changesetProxy, dataOrigin, trans, MaskHandlingForRemovalApplyNoMask.Instance);

            return changed;
        }

        private async Task<bool> WriteRelations(IEnumerable<(T t, Guid ciid)> entities, 
            ISet<(Guid thisCIID, string predicateID)> relevantOutgoingRelations, ISet<(Guid thisCIID, string predicateID)> relevantIncomingRelations,
            string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var changed = false;
            if (!relationFieldInfos.IsEmpty())
            {
                var outgoingRelations = new List<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)>();
                var incomingRelations = new List<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)>();
                foreach (var (t, ciid) in entities)
                {
                    foreach (var trFieldInfo in relationFieldInfos)
                    {
                        var entityValue = trFieldInfo.FieldInfo.GetValue(t);

                        if (entityValue != null)
                        {
                            var otherCIIDs = entityValue as Guid[];
                            if (otherCIIDs == null)
                                throw new Exception(); // invalid type
                            var predicateID = trFieldInfo.TraitRelationAttribute.predicateID;

                            var outgoing = trFieldInfo.TraitRelationAttribute.directionForward;
                            if (outgoing)
                                outgoingRelations.Add((ciid, predicateID, otherCIIDs));
                            else
                                incomingRelations.Add((ciid, predicateID, otherCIIDs));
                        }
                        else
                        {
                            if (!trFieldInfo.TraitRelationAttribute.optional)
                            {
                                throw new Exception(); // TODO
                            }
                        }
                    }
                }

                var tmpChanged = await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayer, outgoingRelations, relevantOutgoingRelations, true), changesetProxy, dataOrigin, trans);
                changed = changed || !tmpChanged.IsEmpty();
                tmpChanged = await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayer, incomingRelations, relevantIncomingRelations, false), changesetProxy, dataOrigin, trans);
                changed = changed || !tmpChanged.IsEmpty();
            }

            return changed;
        }

        public async Task<bool> TryToDelete(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            // TODO: we should actually not fetch a single, but instead ALL that match that ID and delete them
            // only then, the end result is that no CI matching the trait with that ID exists anymore, otherwise it's not guaranteed and deleting fails because there's still a matching ci afterwards
            var dc = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == default)
            {
                return false; // no dc with this ID exists
            }

            await RemoveAttributes(dc.ciid, writeLayerID, dataOrigin, changesetProxy, trans);
            await RemoveRelations(dc.ciid, writeLayerID, dataOrigin, changesetProxy, trans);

            var dcAfterDeletion = await GetSingleByCIID(dc.ciid, layerSet, trans, changesetProxy.TimeThreshold);
            return (dcAfterDeletion == default); // return successful if dc does not exist anymore afterwards
        }

        private async Task RemoveAttributes(Guid ciid, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataCIAndAttributeNameScope(writeLayerID, new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>(), 
                new HashSet<Guid>() { ciid }, attributeFieldInfos.Select(afi => afi.TraitAttributeAttribute.aName).ToHashSet()), 
                changesetProxy, dataOrigin, trans, MaskHandlingForRemovalApplyNoMask.Instance);
        }

        private async Task RemoveRelations(Guid ciid, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (!relationFieldInfos.IsEmpty())
            {
                var outgoing = new HashSet<(Guid thisCIID, string predicateID)>();
                var incoming = new HashSet<(Guid thisCIID, string predicateID)>();
                foreach (var traitRelationField in relationFieldInfos)
                {
                    var predicateID = traitRelationField.TraitRelationAttribute.predicateID;
                    var isOutgoing = traitRelationField.TraitRelationAttribute.directionForward;
                    if (isOutgoing)
                        outgoing.Add((ciid, predicateID));
                    else
                        incoming.Add((ciid, predicateID));
                }

                await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayerID, 
                    new List<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)>(),
                    outgoing, true), changesetProxy, dataOrigin, trans);
                await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayerID, 
                    new List<(Guid thisCIID, string predicateID, IEnumerable<Guid> otherCIIDs)>(),
                    incoming, false), changesetProxy, dataOrigin, trans);
            }
        }
    }

    public class TraitAttributeFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public readonly TraitAttributeAttribute TraitAttributeAttribute;
        public readonly AttributeValueType AttributeValueType;
        public readonly bool IsArray;

        public TraitAttributeFieldInfo(FieldInfo fieldInfo, TraitAttributeAttribute traitAttributeAttribute, AttributeValueType attributeValueType, bool isArray)
        {
            FieldInfo = fieldInfo;
            TraitAttributeAttribute = traitAttributeAttribute;
            AttributeValueType = attributeValueType;
            IsArray = isArray;
        }
    }

    public class TraitRelationFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public readonly TraitRelationAttribute TraitRelationAttribute;

        public TraitRelationFieldInfo(FieldInfo fieldInfo, TraitRelationAttribute traitRelationAttribute)
        {
            FieldInfo = fieldInfo;
            TraitRelationAttribute = traitRelationAttribute;
        }
    }
}
