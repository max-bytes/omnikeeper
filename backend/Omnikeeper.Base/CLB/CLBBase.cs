using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public abstract class CLBBase : IComputeLayerBrain
    {
        protected readonly IAttributeModel attributeModel;
        protected readonly IUserInDatabaseModel userModel;
        protected readonly IChangesetModel changesetModel;
        protected readonly ILayerModel layerModel;

        public CLBBase(IAttributeModel attributeModel, ILayerModel layerModel,
            IChangesetModel changesetModel, IUserInDatabaseModel userModel)
        {
            this.attributeModel = attributeModel;
            this.userModel = userModel;
            this.changesetModel = changesetModel;
            this.layerModel = layerModel;
        }

        public string Name => GetType().Name!;

        public async Task<bool> Run(Layer targetLayer, JObject config, IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            try
            {
                using var trans = modelContextBuilder.BuildDeferred();

                var timeThreshold = TimeThreshold.BuildLatest();

                var username = $"__cl.{Name}"; // make username the same as CLB name
                var displayName = username;
                // generate a unique but deterministic GUID from the clb Name
                var clbUserGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
                var guid = GuidUtility.Create(clbUserGuidNamespace, Name);
                var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);
                var changesetProxy = new ChangesetProxy(user, timeThreshold, changesetModel);

                var errorHandler = new CLBErrorHandler(trans, Name, targetLayer.ID, changesetProxy, attributeModel);

                var result = await Run(targetLayer, config, changesetProxy, errorHandler, trans, logger);

                if (result)
                {
                    await errorHandler.RemoveOutdatedErrors();

                    trans.Commit();
                }

                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Running CLB {Name} failed");
                return false;
            }
        }

        public abstract Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger);

    }
}
