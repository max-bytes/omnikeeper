using Newtonsoft.Json.Linq;
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
    public class ValidationModel : TraitDataConfigBaseModel<Validation, string>, IValidationModel
    {
        public ValidationModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(ValidationTraits.ValidationFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        { }

        //public async Task<Validation> GetValidation(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        //{
        //    return await Get(id, layerSet, timeThreshold, trans);
        //}

        //public async Task<(Guid, Validation)> TryToGetValidation(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        //{
        //    return await TryToGet(id, layerSet, timeThreshold, trans);
        //}

        public async Task<IDictionary<string, Validation>> GetValidations(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        protected override (Validation, string) EffectiveTrait2DC(EffectiveTrait et, MergedCI ci)
        {
            var id = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var ruleName = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "rule_name");
            var ruleConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "rule_config");
            return (new Validation(id, ruleName, ruleConfig), id);
        }

        protected override string EffectiveTrait2ID(EffectiveTrait et, MergedCI ci)
        {
            var id = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            return id;
        }

        public async Task<(Validation validationIssue, bool changed)> InsertOrUpdate(string id, string ruleName, JObject ruleConfig, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("validation.id", new AttributeScalarValueText(id)),
                ("validation.rule_name", new AttributeScalarValueText(ruleName)),
                ("validation.rule_config", AttributeScalarValueJSON.Build(ruleConfig)),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Validation - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "validation.id",
                "validation.rule_name",
                "validation.rule_config",
                ICIModel.NameAttribute
            );
        }


        //new List<TraitAttribute>() {
        //        new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("validation.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
        //        new TraitAttribute("rule", CIAttributeTemplate.BuildFromParams("validation.rule", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
        //        new TraitAttribute("rule_config", CIAttributeTemplate.BuildFromParams("validation.rule_config", AttributeValueType.JSON, false)),
        //    },
        //    new List<TraitAttribute>() {
        //        new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
        //    }
    }
}
