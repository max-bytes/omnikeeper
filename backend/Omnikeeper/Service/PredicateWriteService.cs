using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class PredicateWriteService : IPredicateWriteService
    {
        private readonly ICIModel ciModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IPredicateModel predicateModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public PredicateWriteService(IPredicateModel predicateModel, IBaseConfigurationModel baseConfigurationModel, ICIModel ciModel, 
            IBaseAttributeModel baseAttributeModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.predicateModel = predicateModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        public async Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var t = await predicateModel.TryToGetPredicate(id, changesetProxy.TimeThreshold, trans);

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var changed = false;
            Guid ciid;
            if (t.Equals(default))
            {
                ciid = await ciModel.CreateCI(trans);
                await baseAttributeModel.InsertAttribute("predicate.id", new AttributeScalarValueText(id), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
                changed = true;
            }
            else
            {
                ciid = t.Item1;
            }

            (_, var tmpChanged) = await baseAttributeModel.InsertAttribute("predicate.wordingFrom", new AttributeScalarValueText(wordingFrom), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;
            (_, tmpChanged) = await baseAttributeModel.InsertAttribute("predicate.wordingTo", new AttributeScalarValueText(wordingTo), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;
            var name = $"Predicate - {id}";
            (_, tmpChanged) = await baseAttributeModel.InsertCINameAttribute(name, ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;

            try
            {
                var predicate = await predicateModel.GetPredicate(id, changesetProxy.TimeThreshold, trans);
                return (predicate, changed);
            } catch (Exception)
            {
                throw new Exception("Predicate does not conform to trait requirements");
            }
        }

        public async Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var t = await predicateModel.TryToGetPredicate(id, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no predicate with this ID exists
            }

            await baseAttributeModel.RemoveAttribute("predicate.id", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("predicate.wordingFrom", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("predicate.wordingTo", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("__name", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);

            var tAfterDeletion = await predicateModel.TryToGetPredicate(id, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successfull if predicate does not exist anymore afterwards
        }
    }
}
