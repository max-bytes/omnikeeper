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
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public static class TraitEntityHelper
    {
        public static ((string name, IAttributeValue value, bool isID)[], (string predicateID, bool forward, Guid[] relatedCIIDs)[]) InputDictionary2AttributeAndRelationTuples(IDictionary<string, object?> inputDict, ITrait trait)
        {
            var attributeValues = new List<(string name, IAttributeValue value, bool isID)>();
            var relationValues = new List<(string predicateID, bool forward, Guid[] relatedCIIDs)>();

            foreach (var kv in inputDict)
            {
                var inputFieldName = kv.Key;

                if (kv.Value == null)
                {
                    // input field is specified, but its value is null, so we treat it like it was not specified and skip it
                    continue;
                }

                // lookup value type based on input attribute name
                var attribute = trait.RequiredAttributes.Concat(trait.OptionalAttributes).FirstOrDefault(ra =>
                {
                    var convertedAttributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ra);
                    return convertedAttributeFieldName == inputFieldName;
                });

                if (attribute == null)
                {
                    // lookup relation
                    var relation = trait.OptionalRelations.FirstOrDefault(r =>
                    {
                        var convertedRelationFieldName = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(r);
                        return convertedRelationFieldName == inputFieldName;
                    });

                    if (relation == null)
                    {
                        throw new Exception($"Invalid input field for trait {trait.ID}: {inputFieldName}");
                    }
                    else
                    {
                        var array = (object[])kv.Value;
                        var relatedCIIDs = array.Select(a =>
                        {
                            var relatedCIID = (Guid)a;
                            return relatedCIID;
                        }).ToArray();
                        relationValues.Add((relation.RelationTemplate.PredicateID, relation.RelationTemplate.DirectionForward, relatedCIIDs));
                    }
                }
                else
                {
                    var type = attribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                    IAttributeValue attributeValue = AttributeValueHelper.BuildFromTypeAndObject(type, kv.Value);
                    attributeValues.Add((attribute.AttributeTemplate.Name, attributeValue, attribute.AttributeTemplate.IsID.GetValueOrDefault(false)));
                }
            }

            return (attributeValues.ToArray(), relationValues.ToArray());
        }


        /*
         * NOTE: this does not care whether or not the CI is actually a trait entity or not
         */
        public static async Task<Guid?> GetMatchingCIIDByAttributeValues(IAttributeModel attributeModel, (string name, IAttributeValue value)[] attributeTuples, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code...
            var cisWithIDAttributes = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), NamedAttributesSelection.Build(attributeTuples.Select(t => t.name).ToHashSet()), layerSet, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

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

        public static IDataLoaderResult<ISet<Guid>> GetMatchingCIIDsByRelationFilters(IRelationModel relationModel, ICIIDModel ciidModel, IEnumerable<(TraitRelation traitRelation, RelationFilter filter)> filters, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold, IDataLoaderService dataLoaderService)
        {
            if (filters.IsEmpty())
                throw new Exception("Filtering with empty filter set not supported");

            var relationsDL = dataLoaderService.SetupAndLoadRelation(RelationSelectionWithPredicate.Build(filters.Select(t => t.traitRelation.RelationTemplate.PredicateID)), relationModel, layerSet, timeThreshold, trans);

            return relationsDL.Then(async relations =>
            {
                var relationsLookup = relations.ToLookup(r => r.Relation.PredicateID);

                var isFirstFilter = true;
                ISet<Guid> ret = new HashSet<Guid>();
                foreach (var filter in filters)
                {
                    var template = filter.traitRelation.RelationTemplate;
                    var candidateRelations = relationsLookup[template.PredicateID];
                    var ciidGroupedRelations = (template.DirectionForward) ? candidateRelations.GroupBy(r => r.Relation.FromCIID) : candidateRelations.GroupBy(r => r.Relation.ToCIID);

                    // NOTE: because the set of cis WITH and cis WITHOUT relations are a partition (i.e. have no overlap), we can do these two loops consecutively, yet isFirstFilter is still correct
                    if (filter.filter.RequiresCheckOfCIsWithNonEmptyRelations())
                    {
                        foreach (var t in ciidGroupedRelations)
                        {
                            if (filter.filter.Matches(t))
                            {
                                if (isFirstFilter)
                                    ret.Add(t.Key);
                            }
                            else
                            {
                                if (!isFirstFilter)
                                    ret.Remove(t.Key);
                            }
                        }
                    }
                    if (filter.filter.RequiresCheckOfCIsWithEmptyRelations())
                    {
                        var allCIIDs = await ciidModel.GetCIIDs(trans);
                        var ciidsWithoutRelations = allCIIDs.Except(ciidGroupedRelations.Select(g => g.Key));
                        foreach (var ciidWithoutRelations in ciidsWithoutRelations)
                        {
                            if (filter.filter.Matches(Enumerable.Empty<MergedRelation>()))
                            {
                                if (isFirstFilter)
                                    ret.Add(ciidWithoutRelations);
                            }
                            else
                            {
                                if (!isFirstFilter)
                                    ret.Remove(ciidWithoutRelations);
                            }
                        }
                    }

                    isFirstFilter = false;
                }

                return ret;
            });
        }

        /*
        * NOTE: this does not care whether or not the CIs are actually a trait entities or not
        */
        public static IDataLoaderResult<ISet<Guid>> GetMatchingCIIDsByAttributeFilters(ICIIDSelection ciSelection, IAttributeModel attributeModel, IEnumerable<(TraitAttribute traitAttribute, AttributeScalarTextFilter filter)> filters, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold, IDataLoaderService dataLoaderService)
        {
            if (filters.IsEmpty())
                throw new Exception("Filtering with empty filter set not supported");
            var attributeNames = filters.Select(t => t.traitAttribute.AttributeTemplate.Name);
            // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code...
            return dataLoaderService.SetupAndLoadMergedAttributes(ciSelection, NamedAttributesSelection.Build(attributeNames.ToHashSet()), attributeModel, layerSet, timeThreshold, trans)
                .Then(cisWithAttributes =>
                {
                    ISet<Guid> ret = new HashSet<Guid>();
                    foreach (var t in cisWithAttributes)
                    {
                        var ciid = t.Key;
                        var attributes = t.Value;

                        var meetsAllFilters = true;
                        foreach (var (traitAttribute, filter) in filters)
                        {
                            var attributeName = traitAttribute.AttributeTemplate.Name;
                            if (attributes.TryGetValue(attributeName, out var foundAttribute))
                            {
                                if (!AttributeFilterHelper.Matches(foundAttribute.Attribute.Value, filter))
                                {
                                    meetsAllFilters = false;
                                    break;
                                }
                            }
                            else
                            {
                                meetsAllFilters = false;
                                break;
                            }
                        }
                        if (meetsAllFilters)
                        {
                            ret.Add(ciid);
                        }
                    }
                    return ret;
                });
            
        }
    }
}
