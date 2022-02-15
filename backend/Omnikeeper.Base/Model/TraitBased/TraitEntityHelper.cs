using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
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
        public static ((string name, IAttributeValue value, bool isID)[], (string predicateID, bool forward, Guid[] relatedCIIDs)[]) InputDictionary2AttributeAndRelationTuples(IDictionary<string, object> inputDict, ITrait trait)
        {
            var attributeValues = new List<(string name, IAttributeValue value, bool isID)>();
            var relationValues = new List<(string predicateID, bool forward, Guid[] relatedCIIDs)>();

            foreach (var kv in inputDict)
            {
                var inputFieldName = kv.Key;

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

        public static (string name, IAttributeValue value)[] InputDictionary2IDAttributeTuples(IDictionary<string, object> inputDict, ITrait trait)
        {
            var (attributeValues, _) = InputDictionary2AttributeAndRelationTuples(inputDict, trait);

            return attributeValues.Where(t => t.isID)
                .Select(t => (t.name, t.value))
                .ToArray();
        }


        /*
         * NOTE: this does not care whether or not the CI is actually a trait entity or not
         */
        public static async Task<Guid?> GetMatchingCIIDForTraitEntityByAttributeValueTuples(IAttributeModel attributeModel, (string name, IAttributeValue value)[] attributeValueTuples, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code...
            var cisWithIDAttributes = await attributeModel.GetMergedAttributes(new AllCIIDsSelection(), NamedAttributesSelection.Build(attributeValueTuples.Select(i => i.name).ToHashSet()), layerSet, trans, timeThreshold);

            var foundCIID = cisWithIDAttributes.Where(t =>
            {
                return attributeValueTuples.All(nameValue => {
                    if (t.Value.TryGetValue(nameValue.name, out var ma))
                    {
                        return ma.Attribute.Value.Equals(nameValue.value);
                    }
                    else
                    {
                        return false;
                    }
                });
            })
                .Select(t => t.Key)
                .OrderBy(t => t) // we order by GUID to stay consistent even when multiple CIs would match
                .FirstOrDefault();
            return (foundCIID == default) ? null : foundCIID;
        }
    }
}
