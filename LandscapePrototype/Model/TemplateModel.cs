using Landscape.Base.Model;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Entity.Template;
using LandscapePrototype.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class TemplateModel : ITemplateModel
    {
        private CachedTemplatesProvider TemplatesProvider { get; set; }
        public TemplateModel(CachedTemplatesProvider templatesProvider)
        {
            TemplatesProvider = templatesProvider;
        }

        public async Task<TemplateErrorsCI> CalculateTemplateErrors(string ciid, LayerSet layerset, ICIModel ciModel, NpgsqlTransaction trans)
        {
            var ci = await ciModel.GetMergedCI(ciid, layerset, trans, DateTimeOffset.Now);
            return await CalculateTemplateErrors(ci, trans);
        }

        public async Task<TemplateErrorsCI> CalculateTemplateErrors(MergedCI ci, NpgsqlTransaction trans)
        {
            var templates = await TemplatesProvider.GetTemplates(trans);
            var attributesTemplates = templates.GetAttributesTemplate(ci.Type);

            if (attributesTemplates == null) return TemplateErrorsCI.Build(new Dictionary<string, TemplateErrorsAttribute>());

            return TemplateErrorsCI.Build(
                attributesTemplates.Attributes.Values
                .Select(at => (at.Name, CalculateTemplateErrorsAttribute(ci, at)))
                .Where(t => !t.Item2.Errors.IsEmpty())
                .ToDictionary(t => t.Name, t => t.Item2)
            );
        }

        private IEnumerable<ITemplateErrorAttribute> PerAttributeTemplateChecks(MergedCIAttribute foundAttribute, CIAttributeTemplate at)
        {
            // check required attributes
            if (foundAttribute == null)
            {
                yield return TemplateErrorAttributeMissing.Build($"attribute \"{at.Name}\" {((at.Type.HasValue) ? $" of type \"{at.Type.Value}\" " : "")}is missing!", at.Type);
            } else
            {
                if (at.Type != null && !foundAttribute.Attribute.Value.Type.Equals(at.Type.Value))
                {
                    yield return TemplateErrorAttributeWrongType.Build($"attribute \"{at.Name}\" must have type \"{at.Type.Value}\"!", at.Type.Value);
                }
            }

            // TODO: other checks
        }

        private TemplateErrorsAttribute CalculateTemplateErrorsAttribute(MergedCI ci, CIAttributeTemplate at)
        {
            var foundAttribute = ci.MergedAttributes.FirstOrDefault(a => a.Attribute.Name == at.Name);
            return TemplateErrorsAttribute.Build(at.Name, PerAttributeTemplateChecks(foundAttribute, at));
        }
    }
}
