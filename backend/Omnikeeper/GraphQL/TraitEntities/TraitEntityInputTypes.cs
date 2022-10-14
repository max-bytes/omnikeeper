using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class IDInput
    {
        public readonly (TraitAttribute traitAttribute, IAttributeValue value)[] IDAttributeValues;
        public IDInput((TraitAttribute traitAttribute, IAttributeValue value)[] idAttributeValues)
        {
            IDAttributeValues = idAttributeValues;
        }
    }

    public class IDInputType : InputObjectGraphType<IDInput>
    {
        private readonly ITrait trait;

        public IDInputType(ITrait trait)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityIDInputGraphTypeName(trait);

            foreach (var ta in trait.RequiredAttributes)
            {
                var attributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta);

                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));

                if (ta.AttributeTemplate.IsID.GetValueOrDefault(false))
                {
                    this.AddField(new FieldType()
                    {
                        Name = attributeFieldName,
                        ResolvedType = new NonNullGraphType(graphType)
                    });
                }
            }

            this.trait = trait;
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var (attributeValues, relationValues) = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(value, trait, false, true);

            if (relationValues.Length != 0)
                throw new Exception($"Encountered unexpected input field(s)");

            var t = attributeValues.Where(t => t.traitAttribute.AttributeTemplate.IsID.GetValueOrDefault(false)).ToArray();
            return new IDInput(t);
        }

        public static IDInputType? Build(ITrait at)
        {
            var hasIDFields = at.RequiredAttributes.Any(ra => ra.AttributeTemplate.IsID.GetValueOrDefault(false));
            if (!hasIDFields)
                return null;
            return new IDInputType(at);
        }
    }

    public class RegexOptionsType : EnumerationGraphType<RegexOptions> { }

    public class TextFilterRegexInputType : InputObjectGraphType<TextFilterRegexInput>
    {
        public TextFilterRegexInputType()
        {
            Field("pattern", x => x.Pattern, nullable: false);
            Field("options", x => x.Options, nullable: true, type: typeof(ListGraphType<RegexOptionsType>));
        }
    }

    public class AttributeTextFilterInputType : InputObjectGraphType<AttributeScalarTextFilter>
    {
        public AttributeTextFilterInputType()
        {
            Field("regex", x => x.Regex, nullable: true, type: typeof(TextFilterRegexInputType));
            Field("exact", x => x.Exact, nullable: true);
            Field("isSet", x => x.IsSet, nullable: true);
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var exact = value.TryGetValue("exact", out var e) ? (string?)e : null;
            var regexObj = value.TryGetValue("regex", out var r) ? (TextFilterRegexInput?)r : null;
            var isSet = value.TryGetValue("isSet", out var i) ? (bool?)i : null;

            return AttributeScalarTextFilter.Build(regexObj, exact, isSet);
        }
    }

    public class AttributeBooleanFilterInputType : InputObjectGraphType<AttributeScalarBooleanFilter>
    {
        public AttributeBooleanFilterInputType()
        {
            Field("isTrue", x => x.IsTrue, nullable: true);
            Field("isSet", x => x.IsSet, nullable: true);
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var isTrue = value.TryGetValue("isTrue", out var it) ? (bool?)it : null;
            var isSet = value.TryGetValue("isSet", out var i) ? (bool?)i : null;

            return AttributeScalarBooleanFilter.Build(isTrue, isSet);
        }
    }

    public class RelationFilterInputType : InputObjectGraphType<IInnerRelationFilter[]>
    {
        public RelationFilterInputType()
        {
            Field("exactOtherCIID", typeof(GuidGraphType));
            Field("exactAmount", typeof(UIntGraphType));
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var exactAmount = value.TryGetValue("exactAmount", out var ea) ? (uint?)ea : null;
            var exactOtherCIID = value.TryGetValue("exactOtherCIID", out var eociid) ? (Guid?)eociid : null;

            var ret = new List<IInnerRelationFilter>();
            if (exactAmount != null)
                ret.Add(new ExactAmountInnerRelationFilter(exactAmount.Value));
            if (exactOtherCIID != null)
                ret.Add(new ExactOtherCIIDInnerRelationFilter(exactOtherCIID.Value));
            return ret.ToArray();
        }
    }

    public record class TraitRelationFilterWrapper(FilterInput? Contains);

    public class TraitRelationFilterWrapperType : InputObjectGraphType<TraitRelationFilterWrapper>
    {
        public TraitRelationFilterWrapperType (FilterInputType fit)
        {
            AddField(new FieldType()
            {
                ResolvedType = fit,
                Name = "contains",
            });
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var contains = value.TryGetValue("contains", out var it) ? (FilterInput?)it : null;

            return new TraitRelationFilterWrapper(contains);
        }
    }

    public class FilterInputType : InputObjectGraphType<FilterInput>
    {
        private readonly ITrait trait;
        private readonly IDictionary<string, string> FieldName2AttributeNameMap = new Dictionary<string, string>();
        private readonly IDictionary<string, TraitRelation> FieldName2TraitRelationMap = new Dictionary<string, TraitRelation>();
        private readonly IDictionary<string, (TraitRelation traitRelation, ITrait trait)> FieldName2TraitRelationWithTraitHintsMap = new Dictionary<string, (TraitRelation traitRelation, ITrait trait)>();

        public FilterInputType(ITrait trait)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityFilterInputGraphTypeName(trait);
            this.trait = trait;
        }

        public void Init(IDictionary<string, FilterInputType> allFilters)
        {
            foreach (var ta in trait.RequiredAttributes.Concat(trait.OptionalAttributes))
            {
                var attributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta);

                var attributeValueType = ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                if (!ta.AttributeTemplate.IsArray.GetValueOrDefault(false))
                {
                    switch (attributeValueType)
                    {
                        case AttributeValueType.Text:
                        case AttributeValueType.MultilineText:
                            AddField(new FieldType()
                            {
                                Type = typeof(AttributeTextFilterInputType),
                                Name = attributeFieldName,
                            });
                            FieldName2AttributeNameMap.Add(attributeFieldName, ta.AttributeTemplate.Name);
                            break;
                        case AttributeValueType.Boolean:
                            AddField(new FieldType()
                            {
                                Type = typeof(AttributeBooleanFilterInputType),
                                Name = attributeFieldName,
                            });
                            FieldName2AttributeNameMap.Add(attributeFieldName, ta.AttributeTemplate.Name);
                            break;
                        // TODO: support for non-text types
                        case AttributeValueType.Integer:
                            break;
                        case AttributeValueType.JSON:
                            break;
                        case AttributeValueType.YAML:
                            break;
                        case AttributeValueType.Image:
                            break;
                        case AttributeValueType.Mask:
                            break;
                        case AttributeValueType.Double:
                            break;
                        case AttributeValueType.DateTimeWithOffset:
                            break;
                    }
                }
                else
                {
                    // TODO: support for array types
                }
            }

            foreach (var r in trait.OptionalRelations)
            {
                var relationFieldName = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(r);
                AddField(new FieldType()
                {
                    Type = typeof(RelationFilterInputType),
                    Name = relationFieldName
                });
                FieldName2TraitRelationMap.Add(relationFieldName, r);

                // trait hints
                foreach (var traitIDHint in r.RelationTemplate.TraitHints)
                {
                    if (allFilters.TryGetValue(traitIDHint, out var filterType))
                    {
                        var relationFieldNameTH = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldWithTraitHintName(r, traitIDHint);
                        AddField(new FieldType()
                        {
                            ResolvedType = new TraitRelationFilterWrapperType (filterType),
                            Name = relationFieldNameTH
                        });
                        FieldName2TraitRelationWithTraitHintsMap.Add(relationFieldNameTH, (r, filterType.trait));
                    } else
                    {
                        throw new Exception($"Could not find filter for trait with ID {traitIDHint}");
                    }
                }
            }

            if (Fields.IsEmpty())
            {
                // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement when there are no other fields
                Field<StringGraphType>("placeholder").Resolve(ctx => "placeholder");
            }
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var attributeFilters = new List<AttributeFilter>();
            var relationFilters = new List<RelationFilter>();
            foreach (var kv in value)
            {
                var inputFieldName = kv.Key;

                if (FieldName2AttributeNameMap.TryGetValue(inputFieldName, out var attributeName))
                {
                    if (kv.Value is IAttributeFilter f)
                        attributeFilters.Add(new AttributeFilter(attributeName, f));
                    else
                        throw new Exception($"Unknown attribute filter for attribute {inputFieldName} detected");
                }
                else if (FieldName2TraitRelationMap.TryGetValue(inputFieldName, out var relation))
                {
                    if (kv.Value is not IInnerRelationFilter[] f)
                        throw new Exception($"Unknown relation filter for relation {inputFieldName} detected");
                    relationFilters.AddRange(f.Select(ff => new RelationFilter(relation.RelationTemplate.PredicateID, relation.RelationTemplate.DirectionForward, ff)));
                }
                else if (FieldName2TraitRelationWithTraitHintsMap.TryGetValue(inputFieldName, out var tuple))
                {
                    var (traitRelation, trait) = tuple;
                    if (kv.Value is not TraitRelationFilterWrapper wrapper)
                        throw new Exception($"Unknown relation filter for relation {inputFieldName} detected");

                    if (wrapper.Contains != null)
                        relationFilters.Add(new RelationFilter(traitRelation.RelationTemplate.PredicateID, traitRelation.RelationTemplate.DirectionForward, new RelatedToCIInnerRelationFilter(wrapper.Contains, trait)));
                }
                else
                {
                    throw new Exception($"Could not find input attribute- or relation-filter {inputFieldName} in trait entity {trait.ID}");
                }
            }

            return new FilterInput(attributeFilters.ToArray(), relationFilters.ToArray());
        }
    }

    public class UpsertInput
    {
        public readonly (TraitAttribute traitAttribute, IAttributeValue value)[] AttributeValues;
        public readonly (TraitRelation traitRelation, Guid[] relatedCIIDs)[] RelationValues;
        public UpsertInput((TraitAttribute traitAttribute, IAttributeValue value)[] attributeValues, (TraitRelation traitRelation, Guid[] relatedCIIDs)[] relationValues)
        {
            AttributeValues = attributeValues;
            RelationValues = relationValues;
        }
    }

    public class UpsertInputType : InputObjectGraphType<UpsertInput>
    {
        private readonly ITrait trait;
        private readonly bool isPureUpdate;

        // NOTE: a pure update is one that has less restrictions on what the input must be
        // a pure update does not require that all required attributes or all ID attributes are present
        public UpsertInputType(ITrait trait, bool isPureUpdate)
        {
            Name = TraitEntityTypesNameGenerator.GenerateInsertTraitEntityInputGraphTypeName(trait);

            foreach (var ta in trait.RequiredAttributes)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                var resolvedType = (isPureUpdate) ? graphType : new NonNullGraphType(graphType);
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta),
                    ResolvedType = resolvedType
                });
            }
            foreach (var ta in trait.OptionalAttributes)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta),
                    ResolvedType = graphType
                });
            }
            foreach (var rr in trait.OptionalRelations)
            {
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(rr),
                    ResolvedType = new ListGraphType(new GuidGraphType())
                });
            }

            this.trait = trait;
            this.isPureUpdate = isPureUpdate;
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var t = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(value, trait, throwOnMissingRequiredAttribute: !isPureUpdate, throwOnMissingIDAttribute: !isPureUpdate);
            return new UpsertInput(t.Item1, t.Item2);
        }
    }
}
