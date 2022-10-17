using GraphQL.DataLoader;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public static class TraitEntityHelper
    {
        public static ((TraitAttribute traitAttribute, IAttributeValue? value)[], (TraitRelation traitRelation, Guid[] relatedCIIDs)[]) 
            InputDictionary2AttributeAndRelationTuples(IDictionary<string, object?> inputDict, ITrait trait, bool throwOnMissingRequiredAttribute, bool throwOnMissingIDAttribute)
        {
            var attributeValues = new List<(TraitAttribute traitAttribute, IAttributeValue? value)>();
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
                        attributeValues.Add((attribute, null));
                } else
                {
                    var type = attribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                    IAttributeValue attributeValue = AttributeValueHelper.BuildFromTypeAndObject(type, fittingInputField.Value);
                    attributeValues.Add((attribute, attributeValue));
                }

            }

            foreach(var attribute in trait.OptionalAttributes)
            {
                var convertedAttributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(attribute);
                var fittingInputField = inputDict.FirstOrDefault(kv => convertedAttributeFieldName == kv.Key);
                if (fittingInputField.Equals(default(KeyValuePair<string, object?>)))
                    continue;

                var type = attribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                if (fittingInputField.Value == null) // input field is specified, but its value is null, means remove
                    attributeValues.Add((attribute, null));
                else
                {
                    IAttributeValue attributeValue = AttributeValueHelper.BuildFromTypeAndObject(type, fittingInputField.Value);
                    attributeValues.Add((attribute, attributeValue));
                }
            }

            foreach(var relation in trait.OptionalRelations)
            {
                var convertedRelationFieldName = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(relation);
                var fittingInputField = inputDict.FirstOrDefault(kv => convertedRelationFieldName == kv.Key);
                if (fittingInputField.Equals(default(KeyValuePair<string, object?>)))
                    continue;
                if (fittingInputField.Value == null) // input field is specified, but its value is null, means ignore (empty array means remove)
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

        // returns both the "best" matching CIID according to its corresponding EffectiveTrait (if ci actually fulfills that)
        // when at least one CI is found, the returned ciid is not null, but if none of them fulfill the trait, the returned EffectiveTrait is null
        public static async Task<(Guid, EffectiveTrait?)> GetSingleBestMatchingCI(IEnumerable<Guid> candidateCIIDs, TraitEntityModel traitEntityModel, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (candidateCIIDs.IsEmpty())
                throw new Exception("This method must not be called with an empty CIID list");

            return await GetSingleBestMatchingCI((SpecificCIIDsSelection)SpecificCIIDsSelection.Build(candidateCIIDs.ToHashSet()), traitEntityModel, layerSet, trans, timeThreshold);
        }
        public static async Task<(Guid, EffectiveTrait?)> GetSingleBestMatchingCI(SpecificCIIDsSelection candidateCIIDs, TraitEntityModel traitEntityModel, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: use data loader
            var foundETs = await traitEntityModel.GetByCIID(candidateCIIDs, layerSet, trans, timeThreshold);

            if (foundETs.IsEmpty())
                return (candidateCIIDs.CIIDs.OrderBy(ciid => ciid).First(), null); // we order by GUID to stay consistent even when multiple CIs would match
            else
                return foundETs.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)).First(); // we order by GUID to stay consistent even when multiple CIs would match
        }

        // TODO: this is not only applicable to trait entities -> move to somewhere more general
        /*
        * NOTE: this does not care whether or not the CI is actually a trait entity or not
        */
        public static async Task<IEnumerable<Guid>> GetMatchingCIIDsByAttributeValues(IAttributeModel attributeModel, (string name, IAttributeValue? value)[] attributeTuples, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code...
            var cisWithIDAttributes = await attributeModel.GetMergedAttributes(AllCIIDsSelection.Instance, NamedAttributesSelection.Build(attributeTuples.Select(t => t.name).ToHashSet()), layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

            var foundCIIDs = cisWithIDAttributes.Where(t =>
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
                        if (attributeValue != null) // passed in attribute value was not null, yet CI does not have that attribute -> so it's not a match
                            return false;
                    }
                }
                return true;
            }).Select(kv => kv.Key);
            return foundCIIDs;
        }

        // TODO: this is not only applicable to trait entities -> move to somewhere more general
        /*
        * NOTE: this does not care whether or not the CI is actually a trait entity or not
        */
        public static IDataLoaderResult<ICIIDSelection> GetMatchingCIIDsByRelationFilters(ICIIDSelection ciSelection, 
            IEnumerable<RelationFilter> filters, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold, IDataLoaderService dataLoaderService)
        {
            if (filters.IsEmpty())
                throw new Exception("Filtering with empty filter set not supported");

            // TODO: not very good for performance, escpecially when a ciSelection != All is applied
            var relationsDL = dataLoaderService.SetupAndLoadRelation(RelationSelectionWithPredicate.Build(filters.Select(t => t.PredicateID)), layerSet, timeThreshold, trans);

            return relationsDL.Then(relations =>
            {
                var relationsLookup = relations.ToLookup(r => r.Relation.PredicateID);

                IList<IDataLoaderResult<ICIIDSelection>> dls = new List<IDataLoaderResult<ICIIDSelection>>();
                foreach (var filter in filters)
                {
                    var candidateRelations = relationsLookup[filter.PredicateID];
                    var ciidGroupedRelations = (filter.DirectionForward) ? 
                        candidateRelations.Where(r => ciSelection.Contains(r.Relation.FromCIID)).GroupBy(r => r.Relation.FromCIID) : 
                        candidateRelations.Where(r => ciSelection.Contains(r.Relation.ToCIID)).GroupBy(r => r.Relation.ToCIID);

                    // TODO: performance improvement: after the first filter, we should reduce the input list

                    // NOTE: because the set of cis WITH and cis WITHOUT relations are a partition (i.e. have no overlap), we can do these two loops consecutively
                    if (filter.RequiresCheckOfCIsWithNonEmptyRelations())
                    {
                        dls.Add(filter.MatchAgainstNonEmpty(ciidGroupedRelations, dataLoaderService, layerSet, trans, timeThreshold));
                    }
                    if (filter.RequiresCheckOfCIsWithEmptyRelations())
                    {
                        dls.Add(filter.MatchAgainstEmpty(AllCIIDsExceptSelection.Build(ciidGroupedRelations.Select(g => g.Key).ToHashSet())));
                    }
                }

                return dls
                    .ToResultOfListNonNull()
                    .Then(re => CIIDSelectionExtensions.IntersectAll(re));
            }).ResolveNestedResults();
        }

        // TODO: this is not only applicable to trait entities -> move to somewhere more general
        /*
        * NOTE: this does not care whether or not the CIs are actually a trait entities or not
        */
        public static IDataLoaderResult<ICIIDSelection> GetMatchingCIIDsByAttributeFilters(ICIIDSelection ciSelection, IEnumerable<AttributeFilter> filters, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold, IDataLoaderService dataLoaderService)
        {
            if (filters.IsEmpty())
                throw new Exception("Filtering with empty filter set not supported");

            IDataLoaderResult<ICIIDSelection> result = new DataLoaderResult<ICIIDSelection>(ciSelection);

            foreach (var taFilter in filters)
            {
                // TODO: re-introduce postgres-based implementation for performance improvements, whenever possible
                //var attributeSelection = NamedAttributesWithValueFiltersSelection.Build(new Dictionary<string, AttributeScalarTextFilter>() { { taFilter.attributeName, taFilter.filter } });
                var attributeSelection = NamedAttributesSelection.Build(taFilter.attributeName);

                // NOTE: we reduce the ciSelection with each filter, and in the end, return the resulting ciSelection
                result = result.Then(movingCISelection =>
                {
                    return dataLoaderService.SetupAndLoadMergedAttributes(movingCISelection, attributeSelection, layerSet, timeThreshold, trans)
                    .Then(attributes =>
                    {
                        if (taFilter.filter is AttributeScalarTextFilter tf)
                        {
                            if (tf.IsSet.HasValue && !tf.IsSet.Value)
                            {
                                return movingCISelection.Except(SpecificCIIDsSelection.Build(attributes.Keys.ToHashSet()));
                            }
                            else
                            {
                                var filtered = attributes.Where(a =>
                                {
                                    // check for existance
                                    // NOTE: we expect this method to only be called when the CI contains an attribute with name `filter.attributeName`
                                    // hence, if the IsSet filter is set to "false", we know this filter cannot match
                                    if (tf.IsSet.HasValue && !tf.IsSet.Value)
                                        return false;

                                    var attribute = a.Value[taFilter.attributeName];
                                    var attributeValue = attribute.Attribute.Value;
                                    // type check
                                    if (attributeValue.Type != AttributeValueType.Text && attributeValue.Type != AttributeValueType.MultilineText)
                                        return false;
                                    if (attributeValue.IsArray)
                                        return false;

                                    var v = attributeValue.Value2String();

                                    if (tf.Exact != null)
                                    {
                                        if (v != tf.Exact)
                                            return false;
                                    }
                                    if (tf.Regex != null)
                                    {
                                        if (!tf.Regex.IsMatch(v))
                                            return false;
                                    }
                                    return true;
                                }).Select(kv => kv.Key).ToHashSet();

                                return SpecificCIIDsSelection.Build(filtered);
                            }
                        }
                        else if (taFilter.filter is AttributeScalarBooleanFilter bf)
                        {
                            if (bf.IsSet.HasValue && !bf.IsSet.Value)
                            {
                                return movingCISelection.Except(SpecificCIIDsSelection.Build(attributes.Keys.ToHashSet()));
                            }
                            else
                            {
                                var filtered = attributes.Where(a =>
                                {
                                    // check for existance
                                    // NOTE: we expect this method to only be called when the CI contains an attribute with name `filter.attributeName`
                                    // hence, if the IsSet filter is set to "false", we know this filter cannot match
                                    if (bf.IsSet.HasValue && !bf.IsSet.Value)
                                        return false;

                                    var attribute = a.Value[taFilter.attributeName];
                                    var attributeValue = attribute.Attribute.Value;
                                    // type check
                                    if (attributeValue is not AttributeScalarValueBoolean v)
                                        return false;

                                    if (bf.IsTrue != null)
                                    {
                                        if (v.Value != bf.IsTrue)
                                            return false;
                                    }
                                    return true;
                                }).Select(kv => kv.Key).ToHashSet();

                                return SpecificCIIDsSelection.Build(filtered);
                            }
                        } else
                        {
                            throw new Exception("Invalid filter detected");
                        }
                    });
                }).ResolveNestedResults();
            }

            return result;
        }
    }
}
