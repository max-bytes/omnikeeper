using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class RecursiveTraitModel : GenericTraitEntityModel<RecursiveTrait, string>
    {
        public RecursiveTraitModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel)
            : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }

        public class TraitAttributeSerializer : AttributeJSONSerializer<TraitAttribute>
        {
            public TraitAttributeSerializer() : base(() =>
            {
                return new System.Text.Json.JsonSerializerOptions()
                {
                    Converters = {
                        new JsonStringEnumConverter()
                    },
                    IncludeFields = true
                };
            })
            {
            }
        }

        public class TraitRelationSerializer : AttributeJSONSerializer<TraitRelation>
        {
            public TraitRelationSerializer() : base(() =>
            {
                return new System.Text.Json.JsonSerializerOptions()
                {
                    Converters = {
                        new JsonStringEnumConverter()
                    },
                    IncludeFields = true
                };
            })
            {
            }
        }
    }

    public class InnerLayerDataModel : GenericTraitEntityModel<LayerData, string>
    {
        public InnerLayerDataModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }

    public class PredicateModel : GenericTraitEntityModel<Predicate, string>
    {
        public PredicateModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }

    public class GeneratorV1Model : GenericTraitEntityModel<GeneratorV1, string>
    {
        public GeneratorV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }

    public class CLConfigV1Model : GenericTraitEntityModel<CLConfigV1, string>
    {
        public CLConfigV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }

    public class ValidatorContextV1Model : GenericTraitEntityModel<ValidatorContextV1, string>
    {
        public ValidatorContextV1Model(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }

    public class AuthRoleModel : GenericTraitEntityModel<AuthRole, string>
    {
        public AuthRoleModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }
    }

    public class ChangesetDataModel : GenericTraitEntityModel<ChangesetData, string>
    {
        public ChangesetDataModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel)
        {
        }

        public async Task<Guid> InsertOrUpdateWithAdditionalAttributes(IChangesetProxy changesetProxy, string layerID, IEnumerable<(string name, IAttributeValue value)> additionalAttributes, IModelContext trans)
        {
            var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;

            var correspondingChangeset = await changesetProxy.GetChangeset(layerID, trans);
            // write changeset data
            var (dc, changed, ciid) = await InsertOrUpdate(new ChangesetData(correspondingChangeset.ID.ToString()), new LayerSet(layerID), layerID, changesetProxy, trans, maskHandling);
            // add custom data
            var fragments = additionalAttributes.Select(a => new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, a.name, a.value));
            var attributeNames = additionalAttributes.Select(a => a.name).ToHashSet();
            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataCIAndAttributeNameScope(layerID,
                fragments,
                new HashSet<Guid>() { ciid },
                attributeNames
                ), changesetProxy, trans, maskHandling, otherLayersValueHandling);

            return ciid;
        }
    }
}
