using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class LayerModel : ILayerModel
    {
        public async Task<(Layer layer, bool created)> CreateLayerIfNotExists(string id, IModelContext trans)
        {
            IDValidations.ValidateLayerIDThrow(id);

            var current = await this.GetLayer(id, trans);

            if (current == null)
            {
                // need to create layer
                using (var command = new NpgsqlCommand(@"INSERT INTO layer (id) VALUES (@id)", trans.DBConnection, trans.DBTransaction))
                {
                    command.Parameters.AddWithValue("id", id);
                    await command.ExecuteNonQueryAsync();
                }

                return (Layer.Build(id), true);
            }
            else
            {
                return (current, false);
            }
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            IDValidations.ValidateLayerIDThrow(id);

            try
            {
                using var command = new NpgsqlCommand(@"DELETE FROM layer WHERE id = @id", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("id", id);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (PostgresException)
            {
                return false;
            }
        }

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans)
        {
            var layers = new List<Layer>();
            using (var command = new NpgsqlCommand($@"SELECT l.id FROM layer l", trans.DBConnection, trans.DBTransaction))
            {
                command.Prepare();
                using (var r = await command.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        var id = r.GetString(0);
                        layers.Add(Layer.Build(id));
                    }
                }
            }

            return layers;
        }
    }

    public class LayerDataModel : ILayerDataModel
    {
        private readonly ILayerModel layerModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly GenericTraitEntityModel<LayerData, string> innerModel;

        public LayerDataModel(ILayerModel layerModel, IMetaConfigurationModel metaConfigurationModel, GenericTraitEntityModel<LayerData, string> innerModel)
        {
            this.layerModel = layerModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.innerModel = innerModel;
        }

        public async Task<(LayerData layerData, bool changed)> UpsertLayerData(string id, string description, long color, string state, string clConfigID, string oiaReference, string[] generators, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            IDValidations.ValidateLayerIDThrow(id);

            // sanitize generators
            foreach (var generatorID in generators)
                IDValidations.ValidateGeneratorIDThrow(generatorID);

            var current = await layerModel.GetLayer(id, trans);

            if (current == null)
                throw new Exception("Can only upsert layer-data for existing layer");

            // upsert layer data
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var ld = new LayerData(id, description, color, clConfigID, generators, oiaReference, state);
            var t = await innerModel.InsertOrUpdate(ld, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer, dataOrigin, changesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            return t;
        }

        public async Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            return await innerModel.TryToDelete(id, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer, dataOrigin, changesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);
        }

        public async Task<IDictionary<string, LayerData>> GetLayerData(IModelContext trans, TimeThreshold timeThreshold)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var layerData = await innerModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, timeThreshold);

            // NOTE: we base the returned layer-data on the actually existing layers
            // that means that there can be layer-data entities that will not be returned and
            // that for non-existing layer-data entities, a default entity will be returned
            var layers = await layerModel.GetLayers(trans);
            return layers.Select(l =>
            {
                if (layerData.TryGetValue(l.ID, out var ld))
                {
                    return ld;
                }
                else
                {
                    return new LayerData(l.ID, "", ILayerDataModel.DefaultColor.ToArgb(), "", Array.Empty<string>(), "", ILayerDataModel.DefaultState.ToString());
                }
            }).ToDictionary(ld => ld.LayerID);
        }
    }
}
