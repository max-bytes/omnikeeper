using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public abstract class CLBBase : IComputeLayerBrain
    {
        protected readonly IBaseAttributeModel attributeModel;
        protected readonly IUserInDatabaseModel userModel;
        protected readonly IChangesetModel changesetModel;
        protected readonly ILayerModel layerModel;
        private readonly IPredicateModel predicateModel;
        protected readonly NpgsqlConnection conn;

        public CLBBase(IBaseAttributeModel attributeModel, ILayerModel layerModel, IPredicateModel predicateModel, IChangesetModel changesetModel, IUserInDatabaseModel userModel, NpgsqlConnection conn)
        {
            this.attributeModel = attributeModel;
            this.userModel = userModel;
            this.changesetModel = changesetModel;
            this.conn = conn;
            this.layerModel = layerModel;
            this.predicateModel = predicateModel;
        }

        protected CLBSettings Settings { get; private set; }

        public string Name => GetType().FullName;

        public abstract string[] RequiredPredicates { get; }
        public abstract Trait[] DefinedTraits { get; }

        public async Task<bool> Run(CLBSettings settings, ILogger logger)
        {
            try
            {
                Settings = settings;

                using var trans = await conn.BeginTransactionAsync();

                var atTime = DateTimeOffset.Now;


                var username = Name; // HACK: make username the same as CLB name
                var displayName = username;
                var guid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef1"); // TODO
                var user = await userModel.UpsertUser(username, displayName, guid, UserType.Robot, trans);
                var changesetProxy = ChangesetProxy.Build(user, atTime, changesetModel);

                // prerequisits
                var predicates = await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatestAtTime(atTime), AnchorStateFilter.ActiveOnly);
                var nonExistingRequiredPredicates = RequiredPredicates.Where(rp => !predicates.ContainsKey(rp));

                if (nonExistingRequiredPredicates.Count() > 0)
                {
                    throw new Exception($"The following required predicates are not present: {string.Join(',', nonExistingRequiredPredicates)}");
                }

                var layerSet = await layerModel.BuildLayerSet(new[] { Settings.LayerName }, trans);
                var layer = await layerModel.GetLayer(Settings.LayerName, trans);

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

        public abstract Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, NpgsqlTransaction trans, ILogger logger);

    }
}
