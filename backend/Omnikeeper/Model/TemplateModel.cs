using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using Npgsql;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
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

        //public async Task<TemplateErrorsCI> CalculateTemplateErrors(Guid ciid, LayerSet layerset, ICIModel ciModel, NpgsqlTransaction trans, TimeThreshold atTime)
        //{
        //    var ci = await ciModel.GetMergedCI(ciid, layerset, trans, atTime);
        //    return await CalculateTemplateErrors(ci, trans, atTime);
        //}

        public async Task<TemplateErrorsCI> CalculateTemplateErrors(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: does this even make sense still? we don't have ci-types anymore

            //var templates = await TemplatesProvider.GetTemplates(trans);
            //var template = templates.GetTemplate(ci.Type?.ID);
            //var attributesTemplates = template?.AttributeTemplates;
            //var relationTemplates = template?.RelationTemplates;

            IImmutableDictionary<string, CIAttributeTemplate> attributesTemplates = null;
            IImmutableDictionary<string, RelationTemplate> relationTemplates = null;

            var relationsAndToCIs = (await RelationService.GetMergedRelatedCIs(ci.ID, ci.Layers, CIModel, RelationModel, trans, atTime));

            var errorsAttribute = new Dictionary<string, TemplateErrorsAttribute>();
            if (attributesTemplates != null)
                errorsAttribute = attributesTemplates.Values
                .Select(at => (at.Name, TemplateCheckService.CalculateTemplateErrorsAttribute(ci, at).errors))
                .Where(t => !t.errors.Errors.IsEmpty())
                .ToDictionary(t => t.Name, t => t.errors);
            var errorsRelation = new Dictionary<string, TemplateErrorsRelation>();
            if (relationTemplates != null)
                errorsRelation = relationTemplates.Values
                .Select(rt => (rt.PredicateID, errors: TemplateCheckService.CalculateTemplateErrorsRelation(relationsAndToCIs[rt.PredicateID], rt)))
                .Where(t => !t.errors.Errors.IsEmpty())
                .ToDictionary(t => t.PredicateID, t => t.errors);

            return TemplateErrorsCI.Build(errorsAttribute, errorsRelation);
        }
    }
}
