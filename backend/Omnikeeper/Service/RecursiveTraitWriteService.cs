using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class RecursiveTraitWriteService : IRecursiveTraitWriteService
    {
        private readonly ICIModel ciModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IRecursiveDataTraitModel traitModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public RecursiveTraitWriteService(IRecursiveDataTraitModel traitModel, IBaseConfigurationModel baseConfigurationModel, ICIModel ciModel, 
            IBaseAttributeModel baseAttributeModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.traitModel = traitModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }
        
        public async Task<(RecursiveTrait trait, bool changed)> InsertOrUpdate(
            string id, IEnumerable<TraitAttribute> requiredAttributes, IEnumerable<TraitAttribute>? optionalAttributes, IEnumerable<TraitRelation>? requiredRelations, IEnumerable<string>? requiredTraits,
            DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            if (requiredAttributes.Count() == 0)
                throw new Exception($"Trait must have at least one required attribute");

            var t = await traitModel.TryToGetRecursiveTrait(id, changesetProxy.TimeThreshold, trans);

            Guid ciid = (t.Equals(default)) ? await ciModel.CreateCI(trans) : t.Item1;

            var changed = await TraitConfigDataUtils.WriteAttributes(baseAttributeModel, ciid, writeLayerID, changesetProxy, dataOrigin, trans,
                ("trait.id", new AttributeScalarValueText(id)),
                ("trait.required_attributes", AttributeArrayValueJSON.Build(requiredAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))),
                (optionalAttributes != null) ? ("trait.optional_attributes", AttributeArrayValueJSON.Build(optionalAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))) : default,
                (requiredRelations != null) ? ("trait.required_relations", AttributeArrayValueJSON.Build(optionalAttributes.Select(a => TraitAttribute.Serializer.SerializeToJObject(a)))) : default,
                (requiredTraits != null) ? ("trait.required_traits", AttributeArrayValueText.BuildFromString(requiredTraits)) : default,
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Trait - {id}"))
            );

            var trait = await traitModel.GetRecursiveTrait(id, changesetProxy.TimeThreshold, trans);

            return (trait, changed);
        }

        public async Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var t = await traitModel.TryToGetRecursiveTrait(id, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no trait with this ID exists
            }

            await baseAttributeModel.RemoveAttribute("trait.id", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("trait.required_attributes", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("trait.optional_attributes", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("trait.required_relations", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("trait.required_traits", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("__name", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);

            var tAfterDeletion = await traitModel.TryToGetRecursiveTrait(id, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successfull if trait does not exist anymore afterwards
        }
    }
}
