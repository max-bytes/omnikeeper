using Landscape.Base.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LandscapeRegistry.Service
{
    public class TemplateCheckService
    {
        public static (MergedCIAttribute foundAttribute, TemplateErrorsAttribute errors) CalculateTemplateErrorsAttribute(MergedCI ci, CIAttributeTemplate at)
        {
            var foundAttribute = ci.MergedAttributes.FirstOrDefault(a => a.Attribute.Name == at.Name);
            return (foundAttribute, TemplateErrorsAttribute.Build(at.Name, PerAttributeTemplateChecks(foundAttribute, at)));
        }
        public static (IEnumerable<(Relation relation, MergedCI toCI)> foundRelations, TemplateErrorsRelation errors) CalculateTemplateErrorsRelation(ILookup<string, (Relation relation, MergedCI toCI)> relations, RelationTemplate rt)
        {
            var foundRelations = relations[rt.Predicate.ID].Where(r => rt.ToCITypes.Contains(r.toCI.Type));

            return (foundRelations, TemplateErrorsRelation.Build(rt.Predicate, PerRelationTemplateChecks(foundRelations, rt)));
        }

        private static IEnumerable<ITemplateErrorAttribute> PerAttributeTemplateChecks(MergedCIAttribute foundAttribute, CIAttributeTemplate at)
        {
            // check required attributes
            if (foundAttribute == null)
            {
                yield return TemplateErrorAttributeMissing.Build(at.Name, at.Type);
            }
            else
            {
                if (at.Type != null && (!foundAttribute.Attribute.Value.Type.Equals(at.Type.Value)))
                {
                    yield return TemplateErrorAttributeWrongType.Build(at.Type.Value, foundAttribute.Attribute.Value.Type);
                }
                if (at.IsArray.HasValue && foundAttribute.Attribute.Value.IsArray != at.IsArray.Value)
                {
                    yield return TemplateErrorAttributeWrongMultiplicity.Build(at.IsArray.Value);
                }

                foreach (var c in at.ValueConstraints)
                {
                    var ce = c.CalculateErrors(foundAttribute.Attribute.Value);
                    foreach (var cc in ce) yield return cc;
                }
            }

            // TODO: other checks
        }

        private static IEnumerable<ITemplateErrorRelation> PerRelationTemplateChecks(IEnumerable<(Relation relation, MergedCI toCI)> foundRelations, RelationTemplate rt)
        {
            if (rt.MaxCardinality.HasValue && foundRelations.Count() > rt.MaxCardinality.Value)
                yield return TemplateErrorRelationGeneric.Build($"At most {rt.MaxCardinality.Value} relations with predicate {rt.Predicate.ID} allowed, found {foundRelations.Count()}!");
            if (rt.MinCardinality.HasValue && foundRelations.Count() < rt.MinCardinality.Value)
                yield return TemplateErrorRelationGeneric.Build($"At least {rt.MinCardinality.Value} relations with predicate {rt.Predicate.ID} required, found {foundRelations.Count()}!");
            // TODO: other checks
        }
    }
}
