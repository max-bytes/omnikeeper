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

namespace Omnikeeper.Base.Model
{
    public class GenericTraitEntityModel<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        protected readonly ICIModel ciModel;
        protected readonly IAttributeModel attributeModel;
        protected readonly IRelationModel relationModel;
        private readonly GenericTrait trait;

        //private readonly FieldInfo ciidFieldInfo;
        //private readonly TraitEntityAttribute traitEntityAttribute;
        private readonly IEnumerable<TraitAttributeFieldInfo> attributeFieldInfos;
        private readonly IEnumerable<TraitRelationFieldInfo> relationFieldInfos;

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

            trait = RecursiveTraitService.FlattenSingleRecursiveTrait(TraitBuilderFromClass.Class2RecursiveTrait<T>());

            (_, attributeFieldInfos, relationFieldInfos) = TraitBuilderFromClass.ExtractFieldInfos<T>();

            // TODO: prefetch/calculate idAttribute field infos
        }

        private async Task<(T entity, Guid ciid)> GetSingleByCIID(Guid ciid, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ci = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(ciid), layerSet, false, AllAttributeSelection.Instance, trans, timeThreshold)).FirstOrDefault(); // TODO: reduce attribute via selection, only fetch trait relevant
            if (ci == null) return default;
            var ciWithTrait = await effectiveTraitModel.GetEffectiveTraitForCI(ci, trait, layerSet, trans, timeThreshold);
            if (ciWithTrait == null) return default;
            var dc = TraitBuilderFromClass.EffectiveTrait2Object<T>(ciWithTrait, DefaultSerializer);
            return (dc, ciid);
        }

        // TODO: move ciid out of traitentity, return tuple instead
        public async Task<(T entity, Guid ciid)> GetSingleByDataID(ID id, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var (_, idAttributeName, attributeValueType) = TraitBuilderFromClass.ExtractIDAttributeInfos<T>();

            IAttributeValue idAttributeValue = AttributeValueBuilder.BuildFromTypeAndObject(attributeValueType, id);
            var cisWithIDAttribute = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), NamedAttributesSelection.Build(idAttributeName), layerSet, trans, timeThreshold);
            var foundCIID = cisWithIDAttribute.Where(t => t.Value[idAttributeName].Attribute.Value.Equals(idAttributeValue))
                .Select(t => t.Key)
                .OrderBy(t => t) // we order by GUID to stay consistent even when multiple CIs would match
                .FirstOrDefault();

            if (foundCIID == default)
            { // no fitting entity found
                return default;
            }

            var ret = await GetSingleByCIID(foundCIID, layerSet, trans, timeThreshold);
            return ret;
        }

        public async Task<IDictionary<ID, T>> GetAllByDataID(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var (idField, idAttributeName, attributeValueType) = TraitBuilderFromClass.ExtractIDAttributeInfos<T>();

            var all = await GetAll(layerSet, trans, timeThreshold);
            all.OrderBy(dc => dc.ciid); // we order by GUID to stay consistent even when multiple CIs would match
            var ret = new Dictionary<ID, T>();
            foreach (var dc in all)
            {
                var id = (ID)idField.GetValue(dc.entity);
                if (id == null)
                    throw new Exception(); // TODO: error message
                if (!ret.ContainsKey(id))
                {
                    ret[id] = dc.entity;
                }
            }
            return ret;
        }

        public async Task<IEnumerable<(T entity, Guid ciid)>> GetAll(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, AllAttributeSelection.Instance, trans, timeThreshold); // TODO: reduce attribute via selection, only fetch trait relevant
            var cisWithTrait = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);
            var ret = new List<(T entity, Guid ciid)>();
            foreach (var (ciid, et) in cisWithTrait.Select(kv => (kv.Key, kv.Value)))
            {
                var dc = TraitBuilderFromClass.EffectiveTrait2Object<T>(et, DefaultSerializer);
                ret.Add((dc, ciid));
            }
            return ret;
        }

        public async Task<(T dc, bool changed)> InsertOrUpdate(T t, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, ChangesetProxy changesetProxy, IModelContext trans)
        {
            var (idField, _, _) = TraitBuilderFromClass.ExtractIDAttributeInfos<T>();

            var id = (ID)idField.GetValue(t);
            if (id == null)
                throw new Exception(); // TODO

            var current = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);

            var ciid = (current != default) ? current.ciid : await ciModel.CreateCI(trans);

            var changed = false;

            foreach (var taFieldInfo in attributeFieldInfos)
            {
                var entityValue = taFieldInfo.FieldInfo.GetValue(t);

                if (entityValue != null)
                {
                    var attributeName = taFieldInfo.TraitAttributeAttribute.aName;

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
                            entityValue = JObject.FromObject(entityValue);
                        }
                    }

                    var value = AttributeValueBuilder.BuildFromTypeAndObject(taFieldInfo.AttributeValueType, entityValue);

                    (_, var tmpChanged) = await attributeModel.InsertAttribute(attributeName, value, ciid, writeLayer, changesetProxy, dataOrigin, trans);
                    changed = changed || tmpChanged;
                }
                else
                {
                    if (!taFieldInfo.TraitAttributeAttribute.optional)
                    {
                        throw new Exception(); // TODO
                    }
                }
            }

            foreach (var trFieldInfo in relationFieldInfos)
            {
                var entityValue = trFieldInfo.FieldInfo.GetValue(t);

                if (entityValue != null)
                {
                    var otherCIIDs = entityValue as Guid[];
                    if (otherCIIDs == null)
                        throw new Exception(); // invalid type
                    var predicateID = trFieldInfo.TraitRelationAttribute.predicateID;

                    foreach (var otherCIID in otherCIIDs) {
                        var fromCIID = (trFieldInfo.TraitRelationAttribute.directionForward) ? ciid : otherCIID;
                        var toCIID = (trFieldInfo.TraitRelationAttribute.directionForward) ? otherCIID : ciid;

                        (_, var tmpChanged) = await relationModel.InsertRelation(fromCIID, toCIID, predicateID, writeLayer, changesetProxy, dataOrigin, trans);
                        changed = changed || tmpChanged;
                    }
                }
                else
                {
                    if (!trFieldInfo.TraitRelationAttribute.optional)
                    {
                        throw new Exception(); // TODO
                    }
                }
            }

            var dc = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == default)
                throw new Exception("DC does not conform to trait requirements");
            return (dc.entity, changed);
        }


        public async Task<bool> TryToDelete(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var dc = await GetSingleByDataID(id, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == default)
            {
                return false; // no dc with this ID exists
            }

            foreach (var traitAttributeField in attributeFieldInfos)
            {
                var (_, _) = await attributeModel.RemoveAttribute(traitAttributeField.TraitAttributeAttribute.aName, dc.ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            }

            if (!relationFieldInfos.IsEmpty())
            {
                var allRelationsForward = await relationModel.GetRelations(RelationSelectionFrom.Build(dc.ciid), writeLayerID, trans, TimeThreshold.BuildLatest());
                var allRelationsBackward = await relationModel.GetRelations(RelationSelectionTo.Build(dc.ciid), writeLayerID, trans, TimeThreshold.BuildLatest());
                foreach (var traitRelationField in relationFieldInfos)
                {
                    var predicateID = traitRelationField.TraitRelationAttribute.predicateID;
                    var relevantRelationsForward = allRelationsForward.Where(r => r.PredicateID == predicateID);
                    var relevantRelationsBackward = allRelationsBackward.Where(r => r.PredicateID == predicateID);
                    var relationsToRemove = relevantRelationsForward.Concat(relevantRelationsBackward);

                    foreach (var r in relationsToRemove)
                    {
                        var (_, _) = await relationModel.RemoveRelation(r.FromCIID, r.ToCIID, r.PredicateID, writeLayerID, changesetProxy, dataOrigin, trans);
                    }
                }
            }

            var dcAfterDeletion = await GetSingleByCIID(dc.ciid, layerSet, trans, changesetProxy.TimeThreshold);
            return (dcAfterDeletion == default); // return successful if dc does not exist anymore afterwards
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

    // TODO: refactor
    public static class TraitBuilderFromClass
    {
        public static (TraitEntityAttribute te, IEnumerable<TraitAttributeFieldInfo> ta, IEnumerable<TraitRelationFieldInfo> tr) ExtractFieldInfos<C>() where C : TraitEntity, new()
        {
            Type type = typeof(C);
            var ta = Attribute.GetCustomAttribute(type, typeof(TraitEntityAttribute)) as TraitEntityAttribute;
            if (ta == null)
                throw new Exception($"Could not find attribute TraitEntity on class {type.Name}");

            var attributeFieldInfos = new List<TraitAttributeFieldInfo>();
            var relationFieldInfos = new List<TraitRelationFieldInfo>();
            foreach (FieldInfo fInfo in type.GetFields())
            {
                if (!fInfo.IsStatic) // ignore static fields
                {
                    var taa = Attribute.GetCustomAttribute(fInfo, typeof(TraitAttributeAttribute)) as TraitAttributeAttribute;
                    var tra = Attribute.GetCustomAttribute(fInfo, typeof(TraitRelationAttribute)) as TraitRelationAttribute;
                    if (taa == null && tra == null)
                        throw new Exception($"Trait class {type.Name}: field with neither TraitAttribute nor TraitRelation attribute detected: {fInfo.Name}");
                    else if (taa != null)
                    {
                        var (attributeValueType, isArray) = Type2AttributeValueType(fInfo, taa);
                        attributeFieldInfos.Add(new TraitAttributeFieldInfo(fInfo, taa, attributeValueType, isArray));
                    } else if (tra != null)
                    {
                        relationFieldInfos.Add(new TraitRelationFieldInfo(fInfo, tra));
                    } else
                    {
                        throw new Exception($"Trait class {type.Name}: field with both TraitAttribute AND TraitRelation attribute detected: {fInfo.Name}");
                    }
                }
            }

            return (ta, attributeFieldInfos, relationFieldInfos);
        }

        public static (FieldInfo idField, string attributeName, AttributeValueType attributeValueType) ExtractIDAttributeInfos<C>() where C : TraitEntity, new()
        {
            Type type = typeof(C);
            var idField = type.GetFields().FirstOrDefault(f => Attribute.IsDefined(f, typeof(TraitEntityIDAttribute)));
            if (idField == null)
                throw new Exception("Cannot get trait entity by data ID: class does not specify a TraitEntityID attribute");
            var taa = Attribute.GetCustomAttribute(idField, typeof(TraitAttributeAttribute)) as TraitAttributeAttribute;
            if (taa == null)
                throw new Exception($"Trait class {type.Name}: field without TraitAttribute attribute detected: {idField.Name}");

            var idAttributeName = taa.aName;
            var (attributeValueType, _) = Type2AttributeValueType(idField, taa);

            return (idField, taa.aName, attributeValueType);
        }

        public static C EffectiveTrait2Object<C>(EffectiveTrait et, MyJSONSerializer<object> jsonSerializer) where C : TraitEntity, new()
        {
            var (_, attributeFieldInfos, relationFieldInfos) = ExtractFieldInfos<C>();

            var ret = new C();

            foreach (var taFieldInfo in attributeFieldInfos)
            {
                // get value from effective trait
                if (et.TraitAttributes.TryGetValue(taFieldInfo.TraitAttributeAttribute.taName, out var attribute))
                {
                    var entityFieldValue = attribute.Attribute.Value.ToGenericObject();
                    // support JSON serializer
                    if (taFieldInfo.AttributeValueType == AttributeValueType.JSON && taFieldInfo.TraitAttributeAttribute.isJSONSerialized)
                    {
                        // deserialize before setting field in entity
                        if (taFieldInfo.IsArray)
                        {
                            var fieldType = taFieldInfo.FieldInfo.FieldType.GetElementType();
                            if (fieldType == null)
                                throw new Exception(); // TODO
                            var tokens = (JToken[])entityFieldValue;
                            var deserialized = Array.CreateInstance(fieldType, tokens.Length);
                            for(int i = 0;i < tokens.Length;i++)
                            {
                                var e = jsonSerializer.Deserialize(tokens[i], fieldType);
                                if (e == null)
                                    throw new Exception(); // TODO
                                deserialized.SetValue(e, i);
                            }
                            entityFieldValue = deserialized;
                        }
                        else
                        {
                            var fieldType = taFieldInfo.FieldInfo.FieldType;
                            entityFieldValue = jsonSerializer.Deserialize((JToken)entityFieldValue, fieldType);
                        }
                    }
                    taFieldInfo.FieldInfo.SetValue(ret, entityFieldValue);
                }
                else
                {
                    // optional or not? depending on that, throw error or continue
                    if (!taFieldInfo.TraitAttributeAttribute.optional)
                        throw new Exception($"Could not find trait attribute {taFieldInfo.TraitAttributeAttribute.taName} for mandatory field");
                }
            }

            foreach (var trFieldInfo in relationFieldInfos)
            {
                var isForward = trFieldInfo.TraitRelationAttribute.directionForward;
                var predicateID = trFieldInfo.TraitRelationAttribute.predicateID;
                var trName = trFieldInfo.TraitRelationAttribute.trName;
                var relationList = (isForward) ? et.OutgoingTraitRelations : et.IncomingTraitRelations;
                // get value from effective trait
                if (relationList.TryGetValue(trName, out var relations))
                {
                    var otherCIIDs = ((isForward) ? relations.Select(r => r.Relation.ToCIID) : relations.Select(r => r.Relation.FromCIID)).ToArray();
                    
                    trFieldInfo.FieldInfo.SetValue(ret, otherCIIDs);
                }
                else
                {
                    // optional or not? depending on that, throw error or continue
                    if (!trFieldInfo.TraitRelationAttribute.optional)
                        throw new Exception($"Could not find trait relation {trFieldInfo.TraitRelationAttribute.trName} for mandatory field");
                }
            }

            return ret;
        }

        public static RecursiveTrait Class2RecursiveTrait<C>() where C : TraitEntity, new()
        {
            var (ta, attributeFieldInfos, relationFieldInfos) = ExtractFieldInfos<C>();

            var requiredAttributes = new List<TraitAttribute>();
            var optionalAttributes = new List<TraitAttribute>();
            var requiredRelations = new List<TraitRelation>();
            var optionalRelations = new List<TraitRelation>();

            foreach (var taFieldInfo in attributeFieldInfos)
            {
                var constraints = FieldInfo2AttributeValueConstraints(taFieldInfo.FieldInfo).ToList();
                var taa = taFieldInfo.TraitAttributeAttribute;
                var targetAttributeList = (taa.optional) ? optionalAttributes : requiredAttributes;
                targetAttributeList.Add(new TraitAttribute(taa.taName, new CIAttributeTemplate(taa.aName, taFieldInfo.AttributeValueType, taFieldInfo.IsArray, constraints)));
            }

            foreach (var trFieldInfo in relationFieldInfos)
            {
                var tra = trFieldInfo.TraitRelationAttribute;
                var targetRelationList = (tra.optional) ? optionalRelations : requiredRelations;
                targetRelationList.Add(new TraitRelation(tra.trName, new RelationTemplate(tra.predicateID, tra.directionForward, tra.minCardinality, tra.maxCardinality)));
            }

            var traitOrigin = new TraitOriginV1(ta.originType);

            var ret = new RecursiveTrait(ta.traitName, traitOrigin, requiredAttributes, optionalAttributes, requiredRelations, optionalRelations);
            return ret;
        }

        private static IEnumerable<ICIAttributeValueConstraint> FieldInfo2AttributeValueConstraints(FieldInfo fInfo)
        {
            if (Attribute.GetCustomAttributes(fInfo, typeof(TraitAttributeValueConstraintTextLengthAttribute)) is TraitAttributeValueConstraintTextLengthAttribute[] c)
            {
                foreach (var cc in c)
                {
                    yield return CIAttributeValueConstraintTextLength.Build(cc.Minimum, cc.Maximum);
                }
            }
            if (Attribute.GetCustomAttributes(fInfo, typeof(TraitAttributeValueConstraintTextRegexAttribute)) is TraitAttributeValueConstraintTextRegexAttribute[] r)
            {
                foreach (var rr in r)
                {
                    yield return new CIAttributeValueConstraintTextRegex(rr.RegexStr, rr.RegexOptions);
                }
            }
            if (Attribute.GetCustomAttributes(fInfo, typeof(TraitAttributeValueConstraintArrayLengthAttribute)) is TraitAttributeValueConstraintArrayLengthAttribute[] al)
            {
                foreach (var alal in al)
                {
                    yield return new CIAttributeValueConstraintArrayLength(alal.Minimum, alal.Maximum);
                }
            }
            
            // TODO: support other constraints
        }

        public static (AttributeValueType t, bool isArray) Type2AttributeValueType(FieldInfo fieldInfo, TraitAttributeAttribute taa)
        {
            var fieldType = fieldInfo.FieldType;
            var isArray = fieldType.IsArray;

            AttributeValueType avt;
            if (taa.isJSONSerialized)
            {
                avt = AttributeValueType.JSON;
            }
            else
            {
                var elementType = (isArray) ? fieldType.GetElementType() : fieldType;
                if (elementType == typeof(string))
                    avt = AttributeValueType.Text;
                else if (elementType == typeof(long))
                    avt = AttributeValueType.Integer;
                else if (elementType == typeof(JObject))
                    avt = AttributeValueType.JSON;
                else
                    throw new Exception("Not supported (yet)");
            }

            return (avt, isArray);
        }
    }

}
