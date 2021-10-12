using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginValidation.Validation
{
    public class ValidationIssueModel : IDBasedTraitDataConfigBaseModel<ValidationIssue, string>, IValidationIssueModel
    {
        public ValidationIssueModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(ValidationTraits.ValidationIssueFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        { }

        public async Task<IDictionary<string, ValidationIssue>> GetValidationIssues(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        protected override (ValidationIssue, string) EffectiveTrait2DC(EffectiveTrait et)
        {
            var id = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var message = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "message");
            var affectedRelatedCIs = TraitConfigDataUtils.ExtractMandatoryIncomingRelations(et, "has_issue");
            var affectedCIs = affectedRelatedCIs.Select(r => r.Relation.ToCIID).ToArray();
            return (new ValidationIssue(id, message, affectedCIs), id);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<(ValidationIssue validationIssue, bool changed)> InsertOrUpdate(string id, string message, IEnumerable<Guid> affectedCIs, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributesAndRelations(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                new (string attributeName, IAttributeValue value)[] {
                    ("validation_issue.id", new AttributeScalarValueText(id)),
                    ("validation_issue.message", new AttributeScalarValueText(message)),
                    (ICIModel.NameAttribute, new AttributeScalarValueText($"ValidationIssue - {id}"))
                },
                affectedCIs.Select(affectedCI => (affectedCI, false, "__meta.validation.has_issue"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                new string[] {
                    "validation_issue.id",
                    "validation_issue.message",
                    ICIModel.NameAttribute
                },
                new string[0],
                new string[] { "__meta.validation.has_issue" }
            );
        }
    }
}
