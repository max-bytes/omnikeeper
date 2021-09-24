using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class GeneratorModel : TraitDataConfigBaseModel<GeneratorV1, string>, IGeneratorModel
    {
        public GeneratorModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(CoreTraits.GeneratorFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        {
        }

        public async Task<GeneratorV1> GetGenerator(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateGeneratorIDThrow(id);

            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, GeneratorV1)> TryToGetGenerator(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateGeneratorIDThrow(id);

            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        public async Task<IDictionary<string, GeneratorV1>> GetGenerators(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var generators = await GetAll(layerSet, trans, timeThreshold);
            return generators;
        }

        protected override (GeneratorV1 dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var generatorID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var attributeName = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "attribute_name");
            var attributeValueTemplate = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "attribute_value_template");

            return (new GeneratorV1(generatorID, attributeName, GeneratorAttributeValue.Build(attributeValueTemplate)), generatorID);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<(GeneratorV1 generator, bool changed)> InsertOrUpdate(string id, string attributeName, string attributeValueTemplate, 
            LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("generator.id", new AttributeScalarValueText(id)),
                ("generator.attribute_name", new AttributeScalarValueText(attributeName)),
                ("generator.attribute_value_template", new AttributeScalarValueText(attributeValueTemplate, true)),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Generator - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "generator.id",
                "generator.attribute_name",
                "generator.attribute_value_template",
                ICIModel.NameAttribute
            );
        }
    }
}
