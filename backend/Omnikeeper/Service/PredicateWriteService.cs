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
        private readonly IPredicateModel predicateModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public PredicateWriteService(IPredicateModel predicateModel, IBaseConfigurationModel baseConfigurationModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel)
        {
            this.predicateModel = predicateModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
        }

        public async Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, PredicateConstraints constraints, IChangesetProxy changesetProxy, IModelContext trans, DataOriginV1 dataOrigin)
        {
            var t = await predicateModel.TryToGetPredicate(id, changesetProxy.TimeThreshold, trans);

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

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
            (_, tmpChanged) = await baseAttributeModel.InsertAttribute("__name", new AttributeScalarValueText(name), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;
            // TODO: constraints

            var predicate = await predicateModel.GetPredicate(id, changesetProxy.TimeThreshold, trans);

            return (predicate, changed);
        }

        public async Task<bool> TryToDelete(string id, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            var t = await predicateModel.TryToGetPredicate(id, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no predicate with this ID exists
            }

            await baseAttributeModel.RemoveAttribute("predicate.id", t.Item1, writeLayerID, changesetProxy, trans);
            await baseAttributeModel.RemoveAttribute("predicate.wordingFrom", t.Item1, writeLayerID, changesetProxy, trans);
            await baseAttributeModel.RemoveAttribute("predicate.wordingTo", t.Item1, writeLayerID, changesetProxy, trans);
            await baseAttributeModel.RemoveAttribute("__name", t.Item1, writeLayerID, changesetProxy, trans);

            var tAfterDeletion = await predicateModel.TryToGetPredicate(id, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successfull if predicate does not exist anymore afterwards
        }
    }
}
