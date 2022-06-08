using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class IDInput
    {
        public readonly (string name, IAttributeValue value)[] AttributeValues;
        public IDInput((string name, IAttributeValue value)[] attributeValues)
        {
            AttributeValues = attributeValues;
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
            var (attributeValues, relationValues) = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(value, trait);

            if (relationValues.Length != 0)
                throw new Exception($"Encountered unexpected input field(s)");

            var t = attributeValues.Where(t => t.isID).Select(t => (t.name, t.value)).ToArray();
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

    public class RelationFilterInputType : InputObjectGraphType<InnerRelationFilter>
    {
        public RelationFilterInputType()
        {
            Field("exactAmount", x => x.ExactAmount, nullable: true);
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var exactAmount = value.TryGetValue("exactAmount", out var e) ? (uint?)e : null;

            return InnerRelationFilter.Build(exactAmount);
        }
    }

    public class FilterInput
    {
        public readonly AttributeFilter[] AttributeFilters;
        public readonly RelationFilter[] RelationFilters;

        public FilterInput(AttributeFilter[] attributeFilters, RelationFilter[] relationFilters)
        {
            AttributeFilters = attributeFilters;
            RelationFilters = relationFilters;
        }
    }

    public class FilterInputType : InputObjectGraphType<FilterInput>
    {
        private readonly ITrait trait;
        private readonly IDictionary<string, string> FieldName2AttributeNameMap = new Dictionary<string, string>();
        private readonly IDictionary<string, TraitRelation> FieldName2TraitRelationMap = new Dictionary<string, TraitRelation>();

        public FilterInputType(ITrait trait)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityFilterInputGraphTypeName(trait);

            foreach (var ta in trait.RequiredAttributes.Concat(trait.OptionalAttributes))
            {
                var attributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta);

                // TODO: support for non-text types
                if (ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text) == AttributeValueType.Text)
                {
                    // TODO: support for array types
                    if (!ta.AttributeTemplate.IsArray.GetValueOrDefault(false))
                    {
                        AddField(new FieldType()
                        {
                            Type = typeof(AttributeTextFilterInputType),
                            Name = attributeFieldName,
                        });
                        FieldName2AttributeNameMap.Add(attributeFieldName, ta.AttributeTemplate.Name);
                    }
                }
            }

            foreach (var r in trait.OptionalRelations)
            {
                // TODO: support for trait hints
                var relationFieldName = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(r);
                AddField(new FieldType()
                {
                    Type = typeof(RelationFilterInputType),
                    Name = relationFieldName
                });
                FieldName2TraitRelationMap.Add(relationFieldName, r);
            }

            this.trait = trait;
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
                    if (kv.Value is not AttributeScalarTextFilter f)
                        throw new Exception($"Unknown attribute filter for attribute {inputFieldName} detected");
                    attributeFilters.Add(new AttributeFilter(attributeName, f));
                }
                else if (FieldName2TraitRelationMap.TryGetValue(inputFieldName, out var relation))
                {
                    if (kv.Value is not InnerRelationFilter f)
                        throw new Exception($"Unknown relation filter for relation {inputFieldName} detected");
                    relationFilters.Add(new RelationFilter(relation.RelationTemplate.PredicateID, relation.RelationTemplate.DirectionForward, f));
                }
                else
                {
                    throw new Exception($"Could not find input attribute- or relation-filter {inputFieldName} in trait entity {trait.ID}");
                }
            }

            return new FilterInput(attributeFilters.ToArray(), relationFilters.ToArray());
        }

        public static FilterInputType? Build(ITrait at)
        {
            var t = new FilterInputType(at);
            if (t.Fields.IsEmpty())
                return null;
            return t;
        }
    }

    public class UpsertInput
    {
        public readonly (string name, IAttributeValue value, bool isID)[] AttributeValues;
        public UpsertInput((string name, IAttributeValue value, bool isID)[] attributeValues)
        {
            AttributeValues = attributeValues;
        }
    }

    public class UpsertInputType : InputObjectGraphType<UpsertInput>
    {
        private readonly ITrait trait;

        public UpsertInputType(ITrait trait)
        {
            Name = TraitEntityTypesNameGenerator.GenerateUpsertTraitEntityInputGraphTypeName(trait);

            foreach (var ta in trait.RequiredAttributes)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta),
                    ResolvedType = new NonNullGraphType(graphType)
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

            this.trait = trait;
        }
        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var (attributeValues, relationValues) = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(value, trait);

            if (relationValues.Length != 0)
                throw new Exception($"Encountered unexpected input field(s)");

            return new UpsertInput(attributeValues);
        }
    }

    public class InsertInput
    {
        public readonly (string name, IAttributeValue value, bool isID)[] AttributeValues;
        public readonly (string predicateID, bool forward, Guid[] relatedCIIDs)[] RelationValues;
        public InsertInput((string name, IAttributeValue value, bool isID)[] attributeValues, (string predicateID, bool forward, Guid[] relatedCIIDs)[] relationValues)
        {
            AttributeValues = attributeValues;
            RelationValues = relationValues;
        }
    }

    public class InsertInputType : InputObjectGraphType<InsertInput>
    {
        private readonly ITrait trait;

        public InsertInputType(ITrait trait)
        {
            Name = TraitEntityTypesNameGenerator.GenerateInsertTraitEntityInputGraphTypeName(trait);

            foreach (var ta in trait.RequiredAttributes)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta),
                    ResolvedType = new NonNullGraphType(graphType)
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
        }

        public override object ParseDictionary(IDictionary<string, object?> value)
        {
            var t = TraitEntityHelper.InputDictionary2AttributeAndRelationTuples(value, trait);
            return new InsertInput(t.Item1, t.Item2);
        }
    }
}
