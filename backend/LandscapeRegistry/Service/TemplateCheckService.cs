using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public class TemplateCheckService
    {
        public static IEnumerable<ITemplateErrorAttribute> PerAttributeTemplateChecks(MergedCIAttribute foundAttribute, CIAttributeTemplate at)
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
    }
}
