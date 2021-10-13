using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    // TODO: think about caching?
    public class GeneratorModel : IDBasedTraitDataConfigBaseModel<GeneratorV1, string>, IGeneratorModel
    {
        public static readonly RecursiveTrait Generator = new RecursiveTrait("__meta.config.generator", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("generator.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("attribute_name", CIAttributeTemplate.BuildFromParams("generator.attribute_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("attribute_value_template", CIAttributeTemplate.BuildFromParams("generator.attribute_value_template", AttributeValueType.MultilineText, false)),
            },
            new List<TraitAttribute>()
            {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait GeneratorFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Generator);

        public GeneratorModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(GeneratorFlattened, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        {
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
