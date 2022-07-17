using GraphQL.DataLoader;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public static class TraitEntityHelper
    {
        public static ((TraitAttribute traitAttribute, IAttributeValue value)[], (TraitRelation traitRelation, Guid[] relatedCIIDs)[]) 
            InputDictionary2AttributeAndRelationTuples(IDictionary<string, object?> inputDict, ITrait trait, bool throwOnMissingRequiredAttribute, bool throwOnMissingIDAttribute)
        {
            var attributeValues = new List<(TraitAttribute traitAttribute, IAttributeValue value)>();
            var relationValues = new List<(TraitRelation traitRelation, Guid[] relatedCIIDs)>();

            foreach(var attribute in trait.RequiredAttributes)
            {
                var convertedAttributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(attribute);
                var fittingInputField = inputDict.FirstOrDefault(kv => convertedAttributeFieldName == kv.Key);
                if (fittingInputField.Equals(default(KeyValuePair<string, object?>)))
                {
                    if (attribute.AttributeTemplate.IsID.GetValueOrDefault(false) && throwOnMissingIDAttribute)
                        throw new Exception($"Missing required ID input field {convertedAttributeFieldName} for trait {trait.ID}");
                    else if (throwOnMissingRequiredAttribute)
                        throw new Exception($"Missing required input field {convertedAttributeFieldName} for trait {trait.ID}");
                    else
                        continue;
                }
                if (fittingInputField.Value == null) // input field is specified, but its value is null
                {
                    if (attribute.AttributeTemplate.IsID.GetValueOrDefault(false) && throwOnMissingIDAttribute)
                        throw new Exception($"Required ID input field {convertedAttributeFieldName} for trait {trait.ID} is null");
                    else if (throwOnMissingRequiredAttribute)
                        throw new Exception($"Required input field {convertedAttributeFieldName} for trait {trait.ID} is null");
                    else
                        continue;
                }

                var type = attribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                IAttributeValue attributeValue = AttributeValueHelper.BuildFromTypeAndObject(type, fittingInputField.Value);
                attributeValues.Add((attribute, attributeValue));
            }

            foreach(var attribute in trait.OptionalAttributes)
            {
                var convertedAttributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(attribute);
                var fittingInputField = inputDict.FirstOrDefault(kv => convertedAttributeFieldName == kv.Key);
                if (fittingInputField.Equals(default(KeyValuePair<string, object?>)))
                    continue;
                if (fittingInputField.Value == null) // input field is specified, but its value is null
                    continue;

                var type = attribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                IAttributeValue attributeValue = AttributeValueHelper.BuildFromTypeAndObject(type, fittingInputField.Value);
                attributeValues.Add((attribute, attributeValue));
            }

            foreach(var relation in trait.OptionalRelations)
            {
                var convertedRelationFieldName = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(relation);
                var fittingInputField = inputDict.FirstOrDefault(kv => convertedRelationFieldName == kv.Key);
                if (fittingInputField.Equals(default(KeyValuePair<string, object?>)))
                    continue;
                if (fittingInputField.Value == null) // input field is specified, but its value is null
                    continue;
                var array = (object[])fittingInputField.Value;
                var relatedCIIDs = array.Select(a =>
                {
                    var relatedCIID = (Guid)a;
                    return relatedCIID;
                }).ToArray();
                relationValues.Add((relation, relatedCIIDs));
            }

            return (attributeValues.ToArray(), relationValues.ToArray());
        }


        /*
         * NOTE: this does not care whether or not the CI is actually a trait entity or not
         */
        public static async Task<Guid?> GetMatchingCIIDByAttributeValues(IAttributeModel attributeModel, (string name, IAttributeValue value)[] attributeTuples, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code...
            var cisWithIDAttributes = await attributeModel.GetMergedAttributes(AllCIIDsSelection.Instance, NamedAttributesSelection.Build(attributeTuples.Select(t => t.name).ToHashSet()), layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

            var foundCIID = cisWithIDAttributes.Where(t =>
            {
                for (var i = 0; i < attributeTuples.Length; i++)
                {
                    var attributeName = attributeTuples[i].name;
                    var attributeValue = attributeTuples[i].value;
                    if (t.Value.TryGetValue(attributeName, out var ma))
                    {
                        if (!ma.Attribute.Value.Equals(attributeValue))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            })
                .Select(t => t.Key)
                .OrderBy(t => t) // we order by GUID to stay consistent even when multiple CIs would match
                .FirstOrDefault();
            return (foundCIID == default) ? null : foundCIID;
        }

        public static IDataLoaderResult<ICIIDSelection> GetMatchingCIIDsByRelationFilters(IRelationModel relationModel, ICIIDModel ciidModel, IEnumerable<RelationFilter> filters, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold, IDataLoaderService dataLoaderService)
        {
            if (filters.IsEmpty())
                throw new Exception("Filtering with empty filter set not supported");

            var relationsDL = dataLoaderService.SetupAndLoadRelation(RelationSelectionWithPredicate.Build(filters.Select(t => t.PredicateID)), relationModel, layerSet, timeThreshold, trans);

            return relationsDL.Then(async relations =>
            {
                var relationsLookup = relations.ToLookup(r => r.Relation.PredicateID);

                IList<IDataLoaderResult<IEnumerable<Guid>>> dls = new List<IDataLoaderResult<IEnumerable<Guid>>>();
                foreach (var filter in filters)
                {
                    var candidateRelations = relationsLookup[filter.PredicateID];
                    var ciidGroupedRelations = (filter.DirectionForward) ? candidateRelations.GroupBy(r => r.Relation.FromCIID) : candidateRelations.GroupBy(r => r.Relation.ToCIID);

                    // TODO: performance improvement: after the first filter, we should reduce the input list

                    // NOTE: because the set of cis WITH and cis WITHOUT relations are a partition (i.e. have no overlap), we can do these two loops consecutively
                    if (filter.Filter.RequiresCheckOfCIsWithNonEmptyRelations())
                    {
                        dls.Add(filter.MatchAgainstNonEmpty(ciidGroupedRelations));
                    }
                    if (filter.Filter.RequiresCheckOfCIsWithEmptyRelations())
                    {
                        var allCIIDs = await ciidModel.GetCIIDs(trans); // TODO: use dataloader?
                        var ciidsWithoutRelations = allCIIDs.Except(ciidGroupedRelations.Select(g => g.Key));

                        dls.Add(filter.Filter.MatchAgainstEmpty(ciidsWithoutRelations));
                    }
                }

                var r = dls.ToResultOfListNonNull();
                return r.Then(re =>
                {
                    // taken from https://stackoverflow.com/a/1676684
                    var intersection = re
                        .Skip(1)
                        .Aggregate(
                            (ISet<Guid>)new HashSet<Guid>(re.First()),
                            (h, e) => { h.IntersectWith(e); return h; }
                        );
                    return SpecificCIIDsSelection.Build(intersection.ToImmutableHashSet());
                });
            }).ResolveNestedResults();
        }

        // TODO: this is not only applicable to trait entities -> move to somewhere more general
        /*
        * NOTE: this does not care whether or not the CIs are actually a trait entities or not
        */
        public static async Task<ICIIDSelection> GetMatchingCIIDsByAttributeFilters(ICIIDSelection ciSelection, IAttributeModel attributeModel, IEnumerable<AttributeFilter> filters, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (filters.IsEmpty())
                throw new Exception("Filtering with empty filter set not supported");

            foreach (var taFilter in filters)
            {
                // TODO: re-introduce postgres-based implementation for performance improvements, whenever possible
                //var attributeSelection = NamedAttributesWithValueFiltersSelection.Build(new Dictionary<string, AttributeScalarTextFilter>() { { taFilter.attributeName, taFilter.filter } });
                var attributeSelection = NamedAttributesSelection.Build(taFilter.attributeName);

                // TODO: use dataloader
                var attributes = await attributeModel.GetMergedAttributes(ciSelection, attributeSelection, layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

                // NOTE: we reduce the ciSelection with each filter, and in the end, return the resulting ciSelection
                if (taFilter.filter.IsSet.HasValue && !taFilter.filter.IsSet.Value)
                {
                    ciSelection = ciSelection.Except(SpecificCIIDsSelection.Build(attributes.Keys.ToHashSet()));
                }
                else
                {
                    var filtered = attributes.Where(a =>
                    {
                        // check for existance
                        // NOTE: we expect this method to only be called when the CI contains an attribute with name `filter.attributeName`
                        // hence, if the IsSet filter is set to "false", we know this filter cannot match
                        if (taFilter.filter.IsSet.HasValue && !taFilter.filter.IsSet.Value)
                            return false;

                        var attribute = a.Value[taFilter.attributeName];
                        var attributeValue = attribute.Attribute.Value;
                        // type check
                        if (attributeValue.Type != AttributeValueType.Text && attributeValue.Type != AttributeValueType.MultilineText)
                            return false;
                        if (attributeValue.IsArray)
                            return false;

                        var v = attributeValue.Value2String();

                        if (taFilter.filter.Exact != null)
                        {
                            if (v != taFilter.filter.Exact)
                                return false;
                        }
                        if (taFilter.filter.Regex != null)
                        {
                            if (!taFilter.filter.Regex.IsMatch(v))
                                return false;
                        }
                        return true;
                    }).Select(kv => kv.Key).ToHashSet();

                    ciSelection = SpecificCIIDsSelection.Build(filtered);
                }
            }

            return ciSelection;
        }
    }
}
