using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
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

        protected CLBSettings? Settings { get; private set; }

        public string Name => GetType().FullName!;

        // TODO: turn into data-traits that get created/updated whenever CLB runs?
        public abstract IEnumerable<RecursiveTrait> DefinedTraits { get; }

        private IDictionary<string, GenericTrait> cachedTraits = new Dictionary<string, GenericTrait>();
        protected IDictionary<string, GenericTrait> Traits
        {
            get
            {
                if (cachedTraits == null)
                    cachedTraits = RecursiveTraitService.FlattenRecursiveTraits(DefinedTraits);
                return cachedTraits;
            }
        }

        public async Task<bool> Run(CLBSettings settings, IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            try
            {
                Settings = settings;

                using var trans = modelContextBuilder.BuildDeferred();

                var timeThreshold = TimeThreshold.BuildLatest();

                var username = Name; // make username the same as CLB name
                var displayName = username;
                // generate a unique but deterministic GUID from the clb Name
                var clbUserGuidNamespace = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1");
                var guid = GuidUtility.Create(clbUserGuidNamespace, Name);
                var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);
                var changesetProxy = new ChangesetProxy(user, timeThreshold, changesetModel);

                var layerSet = await layerModel.BuildLayerSet(new[] { Settings.LayerID }, trans);
                var layer = await layerModel.GetLayer(Settings.LayerID, trans);
                if (layer == null)
                    throw new Exception($"Could not find layer with ID {Settings.LayerID}");

                var errorHandler = new CLBErrorHandler(trans, Name, layer.ID, changesetProxy, attributeModel);

                var result = await Run(layer, changesetProxy, errorHandler, trans, logger);

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

        public abstract Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger);

    }
}
