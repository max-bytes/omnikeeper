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
    public class TemplateModel
    {
        private CachedTemplatesProvider TemplatesProvider { get; set; }
        public TemplateModel(CachedTemplatesProvider templatesProvider)
        {
            TemplatesProvider = templatesProvider;
        }

        public async Task UpdateErrorsOfCI(string ciid, long layerID, CIType ciType, CIModel ciModel, long changesetID, NpgsqlTransaction trans)
        {
            if (ciType == null)
            {
                ciType = await ciModel.GetTypeOfCI(ciid, trans, null);
            }

            var templates = await TemplatesProvider.GetTemplates(trans);
            var attributesTemplates = templates.GetAttributesTemplate(ciType, layerID);

            if (attributesTemplates == null) return; // no attributes set, ignore

            var errorFragments = new List<BulkCIAttributeDataCIScope.Fragment>();
            var ci = await ciModel.GetCI(ciid, layerID, trans, DateTimeOffset.Now);

            foreach (var at in attributesTemplates.Attributes.Values)
            {
                var foundAttribute = ci.Attributes.FirstOrDefault(a => a.Name == at.Name);
                // check required attributes
                if (foundAttribute == null)
                {
                    errorFragments.Add(BulkCIAttributeDataCIScope.Fragment.Build($"attribute.{at.Name}.missing", 
                        AttributeValueText.Build($"Attribute \"{at.Name}\" is missing!")));
                }
            }
            await ciModel.BulkReplaceAttributes(BulkCIAttributeDataCIScope.Build("__error.template", layerID, ciid, errorFragments), changesetID, trans);
        }
    }
}
