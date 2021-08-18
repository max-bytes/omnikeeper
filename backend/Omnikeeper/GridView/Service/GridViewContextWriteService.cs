using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Model;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Service
{
    public class GridViewContextWriteService : IGridViewContextWriteService
    {
        private readonly ICIModel ciModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IGridViewContextModel gridViewContextModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public GridViewContextWriteService(IGridViewContextModel gridViewContextModel, IBaseConfigurationModel baseConfigurationModel, ICIModel ciModel, 
            IBaseAttributeModel baseAttributeModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.gridViewContextModel = gridViewContextModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        public async Task<(FullContext context, bool changed)> InsertOrUpdate(string id, string speakingName, string description, GridViewConfiguration configuration, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var t = await gridViewContextModel.TryToGetFullContext(id, changesetProxy.TimeThreshold, trans);

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var changed = false;
            Guid ciid;
            if (t.Equals(default))
            {
                ciid = await ciModel.CreateCI(trans);
                await baseAttributeModel.InsertAttribute("gridview_context.id", new AttributeScalarValueText(id), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
                changed = true;
            }
            else
            {
                ciid = t.Item1;
            }


            (_, var tmpChanged) = await baseAttributeModel.InsertAttribute("gridview_context.config", AttributeScalarValueJSON.Build(GridViewConfiguration.Serializer.SerializeToJObject(configuration)), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;
            (_, tmpChanged) = await baseAttributeModel.InsertAttribute("gridview_context.speaking_name", new AttributeScalarValueText(speakingName), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;
            (_, tmpChanged) = await baseAttributeModel.InsertAttribute("gridview_context.description", new AttributeScalarValueText(description), ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;

            var name = $"Gridview-Context - {id}";
            (_, tmpChanged) = await baseAttributeModel.InsertCINameAttribute(name, ciid, writeLayerID, changesetProxy, dataOrigin, trans);
            changed = changed || tmpChanged;

            var context = await gridViewContextModel.GetFullContext(id, changesetProxy.TimeThreshold, trans);

            return (context, changed);
        }

        public async Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var t = await gridViewContextModel.TryToGetFullContext(id, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no context with this ID exists
            }

            await baseAttributeModel.RemoveAttribute("gridview_context.id", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("gridview_context.config", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("gridview_context.speaking_name", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("gridview_context.description", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("__name", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);

            var tAfterDeletion = await gridViewContextModel.TryToGetFullContext(id, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successfull if context does not exist anymore afterwards
        }
    }
}
