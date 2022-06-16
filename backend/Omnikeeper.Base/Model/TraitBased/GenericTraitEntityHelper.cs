using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Omnikeeper.Base.Model.TraitBased
{
    public static class GenericTraitEntityHelper
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
                        var isID = Attribute.GetCustomAttribute(fInfo, typeof(TraitEntityIDAttribute)) != null;

                        IAttributeJSONSerializer? jsonSerializer = null;
                        if (taa.jsonSerializer != null)
                        {
                            jsonSerializer = (IAttributeJSONSerializer?)Activator.CreateInstance(taa.jsonSerializer);
                            if (jsonSerializer == null)
                                throw new Exception($"Trait class {type.Name}: Field {fInfo.Name} has invalid jsonSerializer");
                        }


                        var (attributeValueType, isArray) = Type2AttributeValueType(fInfo, taa);
                        attributeFieldInfos.Add(new TraitAttributeFieldInfo(fInfo, taa, attributeValueType, isArray, isID, jsonSerializer));
                    }
                    else if (tra != null)
                    {
                        relationFieldInfos.Add(new TraitRelationFieldInfo(fInfo, tra));
                    }
                    else
                    {
                        throw new Exception($"Trait class {type.Name}: field with both TraitAttribute AND TraitRelation attribute detected: {fInfo.Name}");
                    }
                }
            }

            return (ta, attributeFieldInfos, relationFieldInfos);
        }

        public static GenericTraitEntityIDAttributeInfos<C, ID> ExtractIDAttributeInfos<C, ID>() where C : TraitEntity, new() where ID : notnull
        {
            Type type = typeof(C);
            var idFields = type.GetFields().Where(f => Attribute.IsDefined(f, typeof(TraitEntityIDAttribute)));
            if (idFields.Count() == 0)
                throw new Exception("Cannot get trait entity by data ID: class does not specify a TraitEntityID attribute");

            var outFields = new List<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)>();
            foreach (var idField in idFields)
            {
                var taa = Attribute.GetCustomAttribute(idField, typeof(TraitAttributeAttribute)) as TraitAttributeAttribute;
                if (taa == null)
                    throw new Exception($"Trait class {type.Name}: field without TraitAttribute attribute detected: {idField.Name}");

                var idAttributeName = taa.aName;
                var (attributeValueType, _) = Type2AttributeValueType(idField, taa);
                outFields.Add((idField, taa.aName, attributeValueType));
            }
            return new GenericTraitEntityIDAttributeInfos<C, ID>(outFields);
        }

        public static C EffectiveTrait2Object<C>(EffectiveTrait et) where C : TraitEntity, new()
        {
            var (_, attributeFieldInfos, relationFieldInfos) = ExtractFieldInfos<C>();

            return EffectiveTrait2Object<C>(et, attributeFieldInfos, relationFieldInfos);
        }

        public static C EffectiveTrait2Object<C>(EffectiveTrait et, IEnumerable<TraitAttributeFieldInfo> attributeFieldInfos, IEnumerable<TraitRelationFieldInfo> relationFieldInfos) where C : TraitEntity, new()
        {
            var ret = new C();

            foreach (var taFieldInfo in attributeFieldInfos)
            {
                // get value from effective trait
                if (et.TraitAttributes.TryGetValue(taFieldInfo.TraitAttributeAttribute.taName, out var attribute))
                {
                    object entityFieldValue;
                    // support JSON serializer
                    if (taFieldInfo.AttributeValueType == AttributeValueType.JSON && taFieldInfo.JsonSerializer != null)
                    {
                        // deserialize before setting field in entity
                        entityFieldValue = taFieldInfo.JsonSerializer.DeserializeFromAttributeValue(attribute.Attribute.Value);
                    }
                    else
                    {
                        entityFieldValue = attribute.Attribute.Value.ToGenericObject();
                    }
                    taFieldInfo.FieldInfo.SetValue(ret, entityFieldValue);
                }
                else
                {
                    // optional or not? depending on that, throw error or continue
                    if (!taFieldInfo.TraitAttributeAttribute.optional)
                        throw new Exception($"Could not find trait attribute {taFieldInfo.TraitAttributeAttribute.taName} for mandatory field");
                    else
                    {
                        // set to default value, to ensure consistency and not rely on default constructor
                        taFieldInfo.FieldInfo.SetValue(ret, default);
                    }
                }
            }

            foreach (var trFieldInfo in relationFieldInfos)
            {
                var isForward = trFieldInfo.TraitRelationAttribute.directionForward;
                var trName = trFieldInfo.TraitRelationAttribute.trName;
                var relationList = (isForward) ? et.OutgoingTraitRelations : et.IncomingTraitRelations;
                // get value from effective trait
                if (relationList.TryGetValue(trName, out var relations))
                {
                    var otherCIIDs = ((isForward) ? relations.Select(r => r.Relation.ToCIID) : relations.Select(r => r.Relation.FromCIID)).ToArray();

                    if (trFieldInfo.FieldInfo.FieldType.IsArray)
                        trFieldInfo.FieldInfo.SetValue(ret, otherCIIDs);
                    else
                        trFieldInfo.FieldInfo.SetValue(ret, (otherCIIDs.Length == 0) ? null : otherCIIDs.First());
                }
                else
                {
                    // relations are always optional by design, if we do not find it, just continue
                }
            }

            return ret;
        }

        public static RecursiveTrait Class2RecursiveTrait<C>() where C : TraitEntity, new()
        {
            var (ta, attributeFieldInfos, relationFieldInfos) = ExtractFieldInfos<C>();

            var requiredAttributes = new List<TraitAttribute>();
            var optionalAttributes = new List<TraitAttribute>();
            var optionalRelations = new List<TraitRelation>();

            foreach (var taFieldInfo in attributeFieldInfos)
            {
                var constraints = FieldInfo2AttributeValueConstraints(taFieldInfo.FieldInfo).ToList();
                var taa = taFieldInfo.TraitAttributeAttribute;
                var targetAttributeList = (taa.optional) ? optionalAttributes : requiredAttributes;
                targetAttributeList.Add(new TraitAttribute(taa.taName, new CIAttributeTemplate(taa.aName, taFieldInfo.AttributeValueType, taFieldInfo.IsArray, taFieldInfo.IsID, constraints)));
            }

            foreach (var trFieldInfo in relationFieldInfos)
            {
                var tra = trFieldInfo.TraitRelationAttribute;
                optionalRelations.Add(new TraitRelation(tra.trName, new RelationTemplate(tra.predicateID, tra.directionForward, tra.traitHints)));
            }

            var traitOrigin = new TraitOriginV1(ta.originType);

            var ret = new RecursiveTrait(ta.traitName, traitOrigin, requiredAttributes, optionalAttributes, optionalRelations);
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
            if (taa.jsonSerializer != null)
            {
                avt = AttributeValueType.JSON;
            }
            else
            {
                var elementType = (isArray) ? fieldType.GetElementType() : fieldType;
                if (elementType == typeof(string))
                {
                    if (taa.multilineTextHint)
                        avt = AttributeValueType.MultilineText;
                    else
                        avt = AttributeValueType.Text;
                }
                else if (elementType == typeof(long))
                    avt = AttributeValueType.Integer;
                else if (elementType == typeof(double))
                    avt = AttributeValueType.Double;
                else if (elementType == typeof(JsonDocument))
                    avt = AttributeValueType.JSON;
                else
                    throw new Exception("Not supported (yet)");
            }

            return (avt, isArray);
        }
    }

}
