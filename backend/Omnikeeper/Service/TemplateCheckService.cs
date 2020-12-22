using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Service
{
    public class TemplateCheckService
    {
        public static (MergedCIAttribute? foundAttribute, TemplateErrorsAttribute errors) CalculateTemplateErrorsAttribute(MergedCI ci, CIAttributeTemplate at)
        {
            if (ci.MergedAttributes.TryGetValue(at.Name, out var found))
            {
                return (found, new TemplateErrorsAttribute(at.Name, PerAttributeTemplateChecks(found, at)));
            } else
            {
                return (null, new TemplateErrorsAttribute(at.Name, new ITemplateErrorAttribute[] { new TemplateErrorAttributeMissing(at.Name, at.Type) }));
            }
        }
        public static TemplateErrorsRelation CalculateTemplateErrorsRelation(IEnumerable<CompactRelatedCI> relations, RelationTemplate rt)
        {
            return new TemplateErrorsRelation(rt.PredicateID, PerRelationTemplateChecks(relations, rt));
        }

        private static IEnumerable<ITemplateErrorAttribute> PerAttributeTemplateChecks(MergedCIAttribute foundAttribute, CIAttributeTemplate at)
        {
            // check required attributes
            if (at.Type != null && (!foundAttribute.Attribute.Value.Type.Equals(at.Type.Value)))
            {
                yield return TemplateErrorAttributeWrongType.BuildFromSingle(at.Type.Value, foundAttribute.Attribute.Value.Type);
            }
            var isFoundAttributeArray = foundAttribute.Attribute.Value is IAttributeArrayValue;
            if (at.IsArray.HasValue && isFoundAttributeArray != at.IsArray.Value)
            {
                yield return TemplateErrorAttributeWrongMultiplicity.Build(at.IsArray.Value);
            }

            foreach (var c in at.ValueConstraints)
            {
                var ce = c.CalculateErrors(foundAttribute.Attribute.Value);
                foreach (var cc in ce) yield return cc;
            }

            // TODO: other checks
        }

        private static IEnumerable<ITemplateErrorRelation> PerRelationTemplateChecks(IEnumerable<CompactRelatedCI> foundRelations, RelationTemplate rt)
        {
            if (rt.MaxCardinality.HasValue && foundRelations.Count() > rt.MaxCardinality.Value)
                yield return new TemplateErrorRelationGeneric($"At most {rt.MaxCardinality.Value} relations with predicate {rt.PredicateID} allowed, found {foundRelations.Count()}!");
            if (rt.MinCardinality.HasValue && foundRelations.Count() < rt.MinCardinality.Value)
                yield return new TemplateErrorRelationGeneric($"At least {rt.MinCardinality.Value} relations with predicate {rt.PredicateID} required, found {foundRelations.Count()}!");
            // TODO: other checks
        }
    }
}
