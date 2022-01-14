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
        protected readonly ICIModel ciModel;
        private readonly IEnumerable<TraitAttributeFieldInfo> attributeFieldInfos;
        private readonly IEnumerable<TraitRelationFieldInfo> relationFieldInfos;

        private readonly TraitEntityIDAttributeInfos<T, ID> idAttributeInfos;

        private readonly TraitEntityModel traitEntityModel;

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
            this.ciModel = ciModel;

            var trait = RecursiveTraitService.FlattenSingleRecursiveTrait(TraitEntityHelper.Class2RecursiveTrait<T>());

            (_, attributeFieldInfos, relationFieldInfos) = TraitEntityHelper.ExtractFieldInfos<T>();

            idAttributeInfos = TraitEntityHelper.ExtractIDAttributeInfos<T, ID>();

            traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel);
        }

        private async Task<(T entity, Guid ciid)> GetSingleByCIID(Guid ciid, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var et = await traitEntityModel.GetSingleByCIID(ciid, layerSet, trans, timeThreshold);
            if (et == null)
                return default;
            var dc = TraitEntityHelper.EffectiveTrait2Object<T>(et, DefaultSerializer);
            return (dc, ciid);
        }

        private async Task<IDictionary<Guid, T>> GetAllByCIID(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ets = await traitEntityModel.GetAllByCIID(layerSet, trans, timeThreshold);
            return ets.ToDictionary(kv => kv.Key, kv => TraitEntityHelper.EffectiveTrait2Object<T>(kv.Value, DefaultSerializer));
        }

        public async Task<(T entity, Guid ciid)> GetSingleByDataID(ID id, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var idAttributeValueTuples = idAttributeInfos.ExtractAttributeValueTuplesFromID(id);
            var foundCIID = await traitEntityModel.GetSingleCIIDByAttributeValueTuples(idAttributeValueTuples, layerSet, trans, timeThreshold);

            if (!foundCIID.HasValue)
                return default;

            var ret = await GetSingleByCIID(foundCIID.Value, layerSet, trans, timeThreshold);
            return ret;
        }

        public async Task<IDictionary<ID, T>> GetAllByDataID(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ets = (await traitEntityModel.GetAllByDataID(layerSet, trans, timeThreshold))
                .Values
                .OrderBy(et => et.CIID); // we order by CIID to stay consistent even when multiple CIs would match

            var ret = new Dictionary<ID, T>();
            foreach (var et in ets)
            {
                var dc = TraitEntityHelper.EffectiveTrait2Object<T>(et, DefaultSerializer); 
                var id = idAttributeInfos.ExtractIDFromEntity(dc);
                if (!ret.ContainsKey(id))
                {
                    ret[id] = dc;
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
            var attributeFragments = Entities2Fragments(entities);
            var (outgoingRelations, incomingRelations) = Entities2RelationTuples(entities);
            var changed = await traitEntityModel.BulkReplace(relevantCIIDs, attributeFragments, outgoingRelations, incomingRelations, layerSet, writeLayer, dataOrigin, changesetProxy, trans);

            return changed;
        }

        public async Task<(T dc, bool changed)> InsertOrUpdate(T t, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var id = idAttributeInfos.ExtractIDFromEntity(t);

            var current = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);

            var ciid = (current != default) ? current.ciid : await ciModel.CreateCI(trans);

            var tuples = new (T t, Guid ciid)[] { (t, ciid) };
            var attributeFragments = Entities2Fragments(tuples);
            var (outgoingRelations, incomingRelations) = Entities2RelationTuples(tuples);
            var (et, changed) = await traitEntityModel.InsertOrUpdate(ciid, attributeFragments, outgoingRelations, incomingRelations, layerSet, writeLayer, dataOrigin, changesetProxy, trans);

            var dc = TraitEntityHelper.EffectiveTrait2Object<T>(et, DefaultSerializer);

            return (dc, changed);
        }

        private IList<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> Entities2Fragments(IEnumerable<(T t, Guid ciid)> entities)
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

                        var value = AttributeValueHelper.BuildFromTypeAndObject(taFieldInfo.AttributeValueType, entityValue);

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

        private (IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations) Entities2RelationTuples(IEnumerable<(T t, Guid ciid)> entities)
        {
            var outgoingRelations = new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>();
            var incomingRelations = new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>();
            if (!relationFieldInfos.IsEmpty())
            {
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
            }

            return (outgoingRelations, incomingRelations);
        }

        public async Task<bool> TryToDelete(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            // TODO: we should actually not fetch a single, but instead ALL that match that ID and delete them
            // only then, the end result is that no CI matching the trait with that ID exists anymore, otherwise it's not guaranteed and deleting fails because there's still a matching ci afterwards
            var t = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);
            if (t == default)
            {
                return false; // no dc with this ID exists
            }

            return await traitEntityModel.TryToDelete(t.ciid, layerSet, writeLayerID, dataOrigin, changesetProxy, trans);
        }
    }

    public class TraitAttributeFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public readonly TraitAttributeAttribute TraitAttributeAttribute;
        public readonly AttributeValueType AttributeValueType;
        public readonly bool IsArray;
        public readonly bool IsID;

        public TraitAttributeFieldInfo(FieldInfo fieldInfo, TraitAttributeAttribute traitAttributeAttribute, AttributeValueType attributeValueType, bool isArray, bool isID)
        {
            FieldInfo = fieldInfo;
            TraitAttributeAttribute = traitAttributeAttribute;
            AttributeValueType = attributeValueType;
            IsArray = isArray;
            IsID = isID;
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
