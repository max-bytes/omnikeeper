using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class LayerModel : ILayerModel
    {
        public static readonly OnlineInboundAdapterLink DefaultOILP = OnlineInboundAdapterLink.Build("");
        private static readonly AnchorState DefaultState = AnchorState.Active;
        private static readonly Color DefaultColor = Color.White;

        public async Task<Layer> UpsertLayer(string id, IModelContext trans)
        {
            return await UpsertLayer(id, "", DefaultColor, DefaultState, "", DefaultOILP, new string[0], trans);
        }
        public async Task<Layer> UpsertLayer(string id, string description, Color color, AnchorState state, string clConfigID, OnlineInboundAdapterLink oilp, string[] generators, IModelContext trans)
        {
            Debug.Assert(oilp != null);

            IDValidations.ValidateLayerIDThrow(id);

            var current = await GetLayer(id, trans);

            if (current == null)
            {

                // create layer
                using var command = new NpgsqlCommand(@"INSERT INTO layer (id, description) VALUES (@id, @description)", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("description", description); // TODO: move description into own table to make it mutable?
                await command.ExecuteNonQueryAsync();

                // set color
                using var commandColor = new NpgsqlCommand(@"INSERT INTO layer_color (layer_id, color, ""timestamp"")
                        VALUES (@layer_id, @color, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandColor.Parameters.AddWithValue("layer_id", id);
                commandColor.Parameters.AddWithValue("color", color.ToArgb());
                commandColor.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandColor.ExecuteNonQueryAsync();

                // set state
                using var commandState = new NpgsqlCommand(@"INSERT INTO layer_state (layer_id, state, ""timestamp"")
                        VALUES (@layer_id, @state, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandState.Parameters.AddWithValue("layer_id", id);
                commandState.Parameters.AddWithValue("state", state);
                commandState.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandState.ExecuteNonQueryAsync();

                // set clb
                using var commandCLB = new NpgsqlCommand(@"INSERT INTO layer_computelayerbrain (layer_id, brainname, ""timestamp"")
                        VALUES (@layer_id, @brainname, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandCLB.Parameters.AddWithValue("layer_id", id);
                commandCLB.Parameters.AddWithValue("brainname", clConfigID); // TODO: renamed db field
                commandCLB.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandCLB.ExecuteNonQueryAsync();

                // set oilp
                using var commandOILP = new NpgsqlCommand(@"INSERT INTO layer_onlineinboundlayerplugin (layer_id, pluginname, ""timestamp"")
                        VALUES (@layer_id, @pluginname, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandOILP.Parameters.AddWithValue("layer_id", id);
                commandOILP.Parameters.AddWithValue("pluginname", oilp.AdapterName);
                commandOILP.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandOILP.ExecuteNonQueryAsync();

                // set generators
                using var commandGenerators = new NpgsqlCommand(@"INSERT INTO layer_generators (layer_id, generators, ""timestamp"")
                        VALUES (@layer_id, @generators, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandOILP.Parameters.AddWithValue("layer_id", id);
                commandOILP.Parameters.AddWithValue("generators", generators);
                commandOILP.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandOILP.ExecuteNonQueryAsync();

                return Layer.Build(id, description, color, state, clConfigID, oilp, generators);
            }
            else
            {
                // update color
                if (!current.Color.Equals(color))
                {
                    using var commandColor = new NpgsqlCommand(@"INSERT INTO layer_color (layer_id, color, ""timestamp"")
                    VALUES (@layer_id, @color, @timestamp)", trans.DBConnection, trans.DBTransaction);
                    commandColor.Parameters.AddWithValue("layer_id", id);
                    commandColor.Parameters.AddWithValue("color", color.ToArgb());
                    commandColor.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                    await commandColor.ExecuteNonQueryAsync();
                    current = Layer.Build(current.ID, current.Description, color, current.State, current.CLConfigID, current.OnlineInboundAdapterLink, current.Generators);
                }

                // update state
                if (current.State != state)
                {
                    using var commandState = new NpgsqlCommand(@"INSERT INTO layer_state (layer_id, state, ""timestamp"")
                    VALUES (@layer_id, @state, @timestamp)", trans.DBConnection, trans.DBTransaction);
                    commandState.Parameters.AddWithValue("layer_id", id);
                    commandState.Parameters.AddWithValue("state", state);
                    commandState.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                    await commandState.ExecuteNonQueryAsync();
                    current = Layer.Build(current.ID, current.Description, current.Color, state, current.CLConfigID, current.OnlineInboundAdapterLink, current.Generators);
                }

                // update clb
                if (!current.CLConfigID.Equals(clConfigID))
                {
                    using var commandCLB = new NpgsqlCommand(@"INSERT INTO layer_computelayerbrain (layer_id, brainname, ""timestamp"")
                    VALUES (@layer_id, @brainname, @timestamp)", trans.DBConnection, trans.DBTransaction);
                    commandCLB.Parameters.AddWithValue("layer_id", id);
                    commandCLB.Parameters.AddWithValue("brainname", clConfigID);
                    commandCLB.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                    await commandCLB.ExecuteNonQueryAsync();
                    current = Layer.Build(current.ID, current.Description, current.Color, current.State, clConfigID, current.OnlineInboundAdapterLink, current.Generators);
                }

                // update oilp
                if (!current.OnlineInboundAdapterLink.Equals(oilp))
                {
                    using var commandOILP = new NpgsqlCommand(@"INSERT INTO layer_onlineinboundlayerplugin (layer_id, pluginname, ""timestamp"")
                    VALUES (@layer_id, @pluginname, @timestamp)", trans.DBConnection, trans.DBTransaction);
                    commandOILP.Parameters.AddWithValue("layer_id", id);
                    commandOILP.Parameters.AddWithValue("pluginname", oilp.AdapterName);
                    commandOILP.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                    await commandOILP.ExecuteNonQueryAsync();
                    current = Layer.Build(current.ID, current.Description, current.Color, current.State, current.CLConfigID, oilp, current.Generators);
                }

                // update generators
                if (!Enumerable.SequenceEqual(current.Generators, generators))
                {
                    using var commandOILP = new NpgsqlCommand(@"INSERT INTO layer_generators (layer_id, generators, ""timestamp"")
                    VALUES (@layer_id, @generators, @timestamp)", trans.DBConnection, trans.DBTransaction);
                    commandOILP.Parameters.AddWithValue("layer_id", id);
                    commandOILP.Parameters.AddWithValue("generators", generators);
                    commandOILP.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                    await commandOILP.ExecuteNonQueryAsync();
                    current = Layer.Build(current.ID, current.Description, current.Color, current.State, current.CLConfigID, current.OnlineInboundAdapterLink, generators);
                }

                return current;
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

        // TODO: performance improvements!
        public async Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans)
        {
            IDValidations.ValidateLayerIDsThrow(ids);

            foreach (var id in ids)
            {
                using var command = new NpgsqlCommand(@"select id from layer where id = @id LIMIT 1", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("id", id);
                command.Prepare();
                var s = await command.ExecuteScalarAsync();
                if (s == null)
                    throw new Exception(@$"Could not find layer with ID ""{id}""");
            }
            return new LayerSet(ids);
        }

        private async Task<IEnumerable<Layer>> _GetLayers(string whereClause, Action<NpgsqlParameterCollection> addParameters, IModelContext trans)
        {
            var layers = new List<Layer>();
            using var command = new NpgsqlCommand($@"SELECT l.id, l.description, ls.state, lclb.brainname, loilp.pluginname, lc.color, lg.generators FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC NULLS LAST) ls
                    ON ls.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, brainname FROM layer_computelayerbrain ORDER BY layer_id, timestamp DESC NULLS LAST) lclb
                    ON lclb.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, pluginname FROM layer_onlineinboundlayerplugin ORDER BY layer_id, timestamp DESC NULLS LAST) loilp
                    ON loilp.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, color FROM layer_color ORDER BY layer_id, timestamp DESC NULLS LAST) lc
                    ON lc.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, generators FROM layer_generators ORDER BY layer_id, timestamp DESC NULLS LAST) lg
                    ON lg.layer_id = l.id
                WHERE {whereClause}", trans.DBConnection, trans.DBTransaction);
            addParameters(command.Parameters);
            command.Prepare();
            using var r = await command.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetString(0);
                var description = r.GetString(1);
                var state = (r.IsDBNull(2)) ? DefaultState : r.GetFieldValue<AnchorState>(2);
                var clConfig = (r.IsDBNull(3)) ? "" : r.GetString(3);
                var oilp = (r.IsDBNull(4)) ? DefaultOILP : OnlineInboundAdapterLink.Build(r.GetString(4));
                var color = (r.IsDBNull(5)) ? DefaultColor : Color.FromArgb(r.GetInt32(5));
                var generators = (r.IsDBNull(6)) ? new string[0] : r.GetFieldValue<string[]>(6);
                layers.Add(Layer.Build(id, description, color, state, clConfig, oilp, generators));
            }
            return layers;
        }

        public async Task<Layer?> GetLayer(string id, IModelContext trans)
        {
            IDValidations.ValidateLayerIDThrow(id);

            var layers = await _GetLayers("l.id = @id LIMIT 1", (p) => p.AddWithValue("id", id), trans);
            return layers.FirstOrDefault();
        }


        public async Task<IEnumerable<Layer>> GetLayers(IEnumerable<string> layerIDs, IModelContext trans)
        {
            if (layerIDs.IsEmpty()) return new List<Layer>();

            IDValidations.ValidateLayerIDsThrow(layerIDs);

            var layers = (await _GetLayers("l.id = ANY(@layer_ids)", (p) => p.AddWithValue("layer_ids", layerIDs), trans)).ToList();

            // HACK, TODO: wonky re-sorting of layers according to input layerIDs
            return layerIDs.Select(id => layers.Find(l => l.ID == id)).WhereNotNull();
        }

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans)
        {
            var layers = (await _GetLayers("1=1", (p) => { }, trans));
            return layers;
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans)
        {
            var layers = (await _GetLayers("ls.state = ANY(@states) OR (ls.state IS NULL AND @default_state = ANY(@states))",
                (p) =>
                {
                    p.AddWithValue("states", stateFilter.Filter2States());
                    p.AddWithValue("default_state", DefaultState);
                }, trans));
            return layers;
        }
    }
}
