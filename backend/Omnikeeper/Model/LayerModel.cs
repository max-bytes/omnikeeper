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

            var current = await GetLayer(id, trans, TimeThreshold.BuildLatest());

            if (current == null)
            {
                // need to create layer
                using (var command = new NpgsqlCommand(@"INSERT INTO layer (id) VALUES (@id)", trans.DBConnection, trans.DBTransaction))
                {
                    command.Parameters.AddWithValue("id", id);
                    await command.ExecuteNonQueryAsync();
                }

                var @new = await GetLayer(id, trans, TimeThreshold.BuildLatest());

                if (@new == null)
                    throw new Exception("Could not create layer");

                return (@new, true);
            } else
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

        public async Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans)
        {
            IDValidations.ValidateLayerIDsThrow(ids);

            using var command = new NpgsqlCommand(@"select id from layer where id = ANY(@layer_ids)", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("layer_ids", ids);
            command.Prepare();
            using var r = await command.ExecuteReaderAsync();
            var found = new List<string>(ids.Length);
            while (await r.ReadAsync())
            {
                var id = r.GetString(0);
                found.Add(id);
            }
            if (found.Count < ids.Length)
            {
                var notFound = ids.Except(found);
                throw new Exception(@$"Could not find layers with IDs ""{string.Join(",", notFound)}""");
            }
            else
            {
                return new LayerSet(ids);
            }
        }

        private async Task<IEnumerable<Layer>> _GetLayers(string whereClause, Action<NpgsqlParameterCollection> addParameters, IModelContext trans, TimeThreshold timeThreshold)
        {
            var layers = new List<Layer>();
            using (var command = new NpgsqlCommand($@"SELECT l.id FROM layer l WHERE {whereClause}", trans.DBConnection, trans.DBTransaction))
            {
                addParameters(command.Parameters);
                command.Parameters.AddWithValue("time_threshold", timeThreshold.Time);
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

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold)
        {
            var layers = (await _GetLayers("1=1", (p) => { }, trans, timeThreshold));
            return layers;
        }

        public async Task<Layer?> GetLayer(string id, IModelContext trans, TimeThreshold timeThreshold)
        {
            IDValidations.ValidateLayerIDThrow(id);

            var layers = await _GetLayers("l.id = @id LIMIT 1", (p) => p.AddWithValue("id", id), trans, timeThreshold);
            return layers.FirstOrDefault();
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

            var current = await layerModel.GetLayer(id, trans, TimeThreshold.BuildLatest());

            if (current == null)
                throw new Exception("Can only upsert layer-data for existing layer");

            // upsert layer data
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var ld = new LayerData(id, description, color, clConfigID, generators, oiaReference, state);
            var t = await innerModel.InsertOrUpdate(ld, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer, dataOrigin, changesetProxy, trans);
            return t;
        }

        public async Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            return await innerModel.TryToDelete(id, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer, dataOrigin, changesetProxy, trans);
        }

        public async Task<IDictionary<string, LayerData>> GetLayerData(IModelContext trans, TimeThreshold timeThreshold)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var layerData = await innerModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, timeThreshold);

            // NOTE: we base the returned layer-data on the actually existing layers
            // that means that there can be layer-data entities that will not be returned and
            // that for non-existing layer-data entities, a default entity will be returned
            var layers = await layerModel.GetLayers(trans, TimeThreshold.BuildLatest());
            return layers.Select(l =>
            {
                if (layerData.TryGetValue(l.ID, out var ld))
                {
                    return ld;
                } else
                {
                    return new LayerData(l.ID, "", ILayerDataModel.DefaultColor.ToArgb(), "", Array.Empty<string>(), "", ILayerDataModel.DefaultState.ToString()); // TODO: consolidate into default layerstate?
                }
            }).ToDictionary(ld => ld.LayerID);
        }
    }
}
