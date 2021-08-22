using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Threading.Tasks;

namespace OKPluginGenericJSONIngest
{
    public interface IContextWriteService
    {
        Task<(Context context, bool changed)> Upsert(string id, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
        Task<Context> Delete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
    }

    public class ContextWriteService : IContextWriteService
    {
        private readonly ICIModel ciModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IContextModel contextModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public ContextWriteService(IContextModel contextModel, IBaseConfigurationModel baseConfigurationModel, ICIModel ciModel, 
            IBaseAttributeModel baseAttributeModel, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.contextModel = contextModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        public async Task<(Context context, bool changed)> Upsert(string id, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var t = await contextModel.TryToGetContext(id, changesetProxy.TimeThreshold, trans);

            Guid ciid = (t.Equals(default)) ? await ciModel.CreateCI(trans) : t.Item1;

            var changed = await TraitConfigDataUtils.WriteAttributes(baseAttributeModel, ciid, writeLayerID, changesetProxy, dataOrigin, trans,
                ("gji_context.id", new AttributeScalarValueText(id)),
                ("gji_context.extract_config", AttributeScalarValueJSON.Build(Context.ExtractConfigSerializer.SerializeToJObject(extractConfig))),
                ("gji_context.transform_config", AttributeScalarValueJSON.Build(Context.TransformConfigSerializer.SerializeToJObject(transformConfig))),
                ("gji_context.load_config", AttributeScalarValueJSON.Build(Context.LoadConfigSerializer.SerializeToJObject(loadConfig))),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Generic-JSON-Ingest-Context - {id}"))
            );

            try
            {
                var context = await contextModel.GetContext(id, changesetProxy.TimeThreshold, trans);
                return (context, changed);
            } catch (Exception)
            {
                throw new Exception("Context does not conform to trait requirements");
            }
        }

        public async Task<Context> Delete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans)
        {
            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
            var writeLayerID = baseConfiguration.ConfigWriteLayer;

            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                throw new Exception($"User \"{user.Username}\" does not have permission to write to layer {writeLayerID}");

            var t = await contextModel.TryToGetContext(id, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"No context with ID {id} exists");
            }

            await baseAttributeModel.RemoveAttribute("gji_context.id", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("gji_context.extract_config", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("gji_context.transform_config", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("gji_context.load_config", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            await baseAttributeModel.RemoveAttribute("__name", t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);

            var tAfterDeletion = await contextModel.TryToGetContext(id, changesetProxy.TimeThreshold, trans);
            if (!tAfterDeletion.Equals(default))
            {
                throw new Exception($"Could not delete context with ID {id}");
            }
            return t.Item2;
        }
    }
}
