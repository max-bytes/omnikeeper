using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Service
{
    public class TemplateCheckService
    {
        public static (MergedCIAttribute? foundAttribute, bool hasErrors) CalculateTemplateErrorsAttributeSimple(MergedCI ci, CIAttributeTemplate at)
        {
            if (ci.MergedAttributes.TryGetValue(at.Name, out var found))
            {
                return (found, PerAttributeTemplateChecksSimple(found, at));
            }
            else
            {
                return (null, true);
            }
        }
        public static (IEnumerable<MergedRelation>? relevantRelations, bool hasErrors) CalculateTemplateErrorsRelationSimple(IEnumerable<MergedRelation> fromRelations, IEnumerable<MergedRelation> toRelations, RelationTemplate rt)
        {
            var relevantRelations = (rt.DirectionForward) ? fromRelations : toRelations;
            relevantRelations = relevantRelations.Where(r => r.Relation.PredicateID == rt.PredicateID);
            return (relevantRelations, false);
        }

        private static bool PerAttributeTemplateChecksSimple(MergedCIAttribute foundAttribute, CIAttributeTemplate at)
        {
            // check required attributes
            if (at.Type != null && (!foundAttribute.Attribute.Value.Type.Equals(at.Type.Value)))
            {
                return true;
            }
            var isFoundAttributeArray = foundAttribute.Attribute.Value is IAttributeArrayValue;
            if (at.IsArray.HasValue && isFoundAttributeArray != at.IsArray.Value)
            {
                return true;
            }

            foreach (var c in at.ValueConstraints)
            {
                if (c.HasErrors(foundAttribute.Attribute.Value))
                    return true;
            }

            // TODO: other checks

            return false;
        }
    }
}
