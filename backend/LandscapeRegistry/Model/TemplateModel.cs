using Landscape.Base.Entity;
using Landscape.Base.Model;
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
        private IRelationModel RelationModel { get; set; }
        private ICIModel CIModel { get; set; }
        public TemplateModel(ITemplatesProvider templatesProvider, IRelationModel relationModel, ICIModel ciModel)
        {
            TemplatesProvider = templatesProvider;
            RelationModel = relationModel;
            CIModel = ciModel;
        }

        public async Task<TemplateErrorsCI> CalculateTemplateErrors(Guid ciid, LayerSet layerset, ICIModel ciModel, NpgsqlTransaction trans)
        {
            var ci = await ciModel.GetMergedCI(ciid, layerset, trans, DateTimeOffset.Now);
            return await CalculateTemplateErrors(ci, trans);
        }

        public async Task<TemplateErrorsCI> CalculateTemplateErrors(MergedCI ci, NpgsqlTransaction trans)
        {
            var templates = await TemplatesProvider.GetTemplates(trans);
            var template = templates.GetTemplate(ci.Type?.ID);
            var attributesTemplates = template?.AttributeTemplates;
            var relationTemplates = template?.RelationTemplates;

            var relationsAndToCIs = (await RelationService.GetMergedForwardRelationsAndToCIs(ci, CIModel, RelationModel, trans))
                .ToLookup(t => t.relation.PredicateID);

            var errorsAttribute = new Dictionary<string, TemplateErrorsAttribute>();
            if (attributesTemplates != null)
                errorsAttribute = attributesTemplates.Values
                .Select(at => (at.Name, TemplateCheckService.CalculateTemplateErrorsAttribute(ci, at).errors))
                .Where(t => !t.errors.Errors.IsEmpty())
                .ToDictionary(t => t.Name, t => t.errors);
            var errorsRelation = new Dictionary<string, TemplateErrorsRelation>();
            if (relationTemplates != null)
                errorsRelation = relationTemplates.Values
                .Select(rt => (rt.PredicateID, TemplateCheckService.CalculateTemplateErrorsRelation(relationsAndToCIs, rt).errors))
                .Where(t => !t.errors.Errors.IsEmpty())
                .ToDictionary(t => t.PredicateID, t => t.errors);

            return TemplateErrorsCI.Build(errorsAttribute, errorsRelation);
        }
    }
}
