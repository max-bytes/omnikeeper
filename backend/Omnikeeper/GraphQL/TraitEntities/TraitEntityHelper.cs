using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL.TraitEntities
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
    }
}
