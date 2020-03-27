using Landscape.Base.Model;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Entity.Template;
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

        public async Task UpdateErrorsOfLayer(long layerID, ICIModel ciModel, long changesetID, NpgsqlTransaction trans)
        {
            var templates = await TemplatesProvider.GetTemplates(trans);

            var errorFragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            var cis = await ciModel.GetCIs(layerID, true, trans, DateTimeOffset.Now);
            foreach (var ci in cis)
            {
                var attributesTemplates = templates.GetAttributesTemplate(ci.Type, layerID);
                if (attributesTemplates == null) continue; // no attributes set, ignore
                foreach (var at in attributesTemplates.Attributes.Values)
                {
                    errorFragments.AddRange(PerAttributeTemplateChecks(ci, at).Select(t => BulkCIAttributeDataLayerScope.Fragment.Build(t.Item1, t.Item2, ci.Identity)));
                }
            }
            await ciModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("__error.template", layerID, errorFragments), changesetID, trans);
        }

        public async Task UpdateErrorsOfCI(string ciid, long layerID, ICIModel ciModel, long changesetID, NpgsqlTransaction trans)
        {
            var ci = await ciModel.GetCI(ciid, layerID, trans, DateTimeOffset.Now);

            var templates = await TemplatesProvider.GetTemplates(trans);
            var attributesTemplates = templates.GetAttributesTemplate(ci.Type, layerID);
            if (attributesTemplates == null) return; // no attributes set, ignore

            var errorFragments = new List<BulkCIAttributeDataCIScope.Fragment>();
            foreach (var at in attributesTemplates.Attributes.Values)
            {
                errorFragments.AddRange(PerAttributeTemplateChecks(ci, at).Select(t => BulkCIAttributeDataCIScope.Fragment.Build(t.Item1, t.Item2)));
            }
            await ciModel.BulkReplaceAttributes(BulkCIAttributeDataCIScope.Build("__error.template", layerID, ciid, errorFragments), changesetID, trans);
        }

        private IEnumerable<(string, IAttributeValue)> PerAttributeTemplateChecks(CI ci, CIAttributeTemplate at)
        {
            var foundAttribute = ci.Attributes.FirstOrDefault(a => a.Name == at.Name);
            // check required attributes
            if (foundAttribute == null)
            {
                yield return ($"attribute.{at.Name}.missing", AttributeValueText.Build($"Attribute \"{at.Name}\" is missing!"));
            } else
            {
                if (at.Type != null && !foundAttribute.Value.Type.Equals(at.Type.Value))
                {
                    yield return ($"attribute.{at.Name}.wrongType", AttributeValueText.Build($"Attribute \"{at.Name}\" must have type \"{at.Type.Value}\"!"));
                }
            }

            // TODO: other checks
        }
    }
}
