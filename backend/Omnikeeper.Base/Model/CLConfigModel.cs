using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    // TODO: think about caching?
    public class CLConfigModel : TraitDataConfigBaseModel<CLConfigV1, string>, ICLConfigModel
    {
        public static readonly RecursiveTrait CLConfig = new RecursiveTrait("__meta.config.cl_config", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("cl_config.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("cl_brain_reference", CIAttributeTemplate.BuildFromParams("cl_config.cl_brain_reference", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("cl_brain_config", CIAttributeTemplate.BuildFromParams("cl_config.cl_brain_config", AttributeValueType.JSON, false)),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait CLConfigFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(CLConfig);

        public CLConfigModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(CLConfigFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        {
        }

        public async Task<CLConfigV1> GetCLConfig(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateGeneratorIDThrow(id);

            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, CLConfigV1)> TryToGetCLConfig(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateCLConfigIDThrow(id);

            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        public async Task<IDictionary<string, CLConfigV1>> GetCLConfigs(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        protected override (CLConfigV1 dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var id = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var clBrainReference = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "cl_brain_reference");
            var clBrainConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "cl_brain_config");

            return (new CLConfigV1(id, clBrainReference, clBrainConfig), id);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<(CLConfigV1 config, bool changed)> InsertOrUpdate(string id, string clBrainReference, JObject clBrainConfig, 
            LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("cl_config.id", new AttributeScalarValueText(id)),
                ("cl_config.cl_brain_reference", new AttributeScalarValueText(clBrainReference)),
                ("cl_config.cl_brain_config", AttributeScalarValueJSON.Build(clBrainConfig)),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"CL-Config - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "cl_config.id",
                "cl_config.cl_brain_reference",
                "cl_config.cl_brain_config",
                ICIModel.NameAttribute
            );
        }
    }
}
