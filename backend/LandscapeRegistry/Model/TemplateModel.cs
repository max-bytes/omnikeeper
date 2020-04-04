using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TemplateModel : ITemplateModel
    {
        private ITemplatesProvider TemplatesProvider { get; set; }
        public TemplateModel(ITemplatesProvider templatesProvider)
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
            var attributesTemplates = templates.GetTemplate(ci.Type)?.AttributeTemplates;

            if (attributesTemplates == null) return TemplateErrorsCI.Build(new Dictionary<string, TemplateErrorsAttribute>());

            return TemplateErrorsCI.Build(
                attributesTemplates.Values
                .Select(at => (at.Name, CalculateTemplateErrorsAttribute(ci, at)))
                .Where(t => !t.Item2.Errors.IsEmpty())
                .ToDictionary(t => t.Name, t => t.Item2)
            );
        }


        private TemplateErrorsAttribute CalculateTemplateErrorsAttribute(MergedCI ci, CIAttributeTemplate at)
        {
            var foundAttribute = ci.MergedAttributes.FirstOrDefault(a => a.Attribute.Name == at.Name);
            return TemplateErrorsAttribute.Build(at.Name, TemplateCheckService.PerAttributeTemplateChecks(foundAttribute, at));
        }
    }
}
