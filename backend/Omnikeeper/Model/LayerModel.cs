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
        public static readonly ComputeLayerBrainLink DefaultCLB = ComputeLayerBrainLink.Build("");
        public static readonly OnlineInboundAdapterLink DefaultOILP = OnlineInboundAdapterLink.Build("");
        private static readonly AnchorState DefaultState = AnchorState.Active;
        private static readonly Color DefaultColor = Color.White;

        public async Task<Layer> CreateLayer(string name, IModelContext trans)
        {
            return await CreateLayer(name, DefaultColor, DefaultState, DefaultCLB, DefaultOILP, trans);
        }
        public async Task<Layer> CreateLayer(string name, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans)
        {
            Debug.Assert(computeLayerBrain != null);
            Debug.Assert(oilp != null);

            // create layer
            using var command = new NpgsqlCommand(@"INSERT INTO layer (name) VALUES (@name) returning id", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("name", name); // TODO: move layername into own table to make it mutable
            var id = (long)await command.ExecuteScalarAsync();

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
            commandCLB.Parameters.AddWithValue("brainname", computeLayerBrain.Name);
            commandCLB.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
            await commandCLB.ExecuteNonQueryAsync();

            // set oilp
            using var commandOILP = new NpgsqlCommand(@"INSERT INTO layer_onlineinboundlayerplugin (layer_id, pluginname, ""timestamp"")
                    VALUES (@layer_id, @pluginname, @timestamp)", trans.DBConnection, trans.DBTransaction);
            commandOILP.Parameters.AddWithValue("layer_id", id);
            commandOILP.Parameters.AddWithValue("pluginname", oilp.AdapterName);
            commandOILP.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
            await commandOILP.ExecuteNonQueryAsync();

            return Layer.Build(name, id, color, state, computeLayerBrain, oilp);
        }

        public async Task<Layer> Update(long id, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans)
        {
            Debug.Assert(computeLayerBrain != null);
            Debug.Assert(oilp != null);

            var current = await GetLayer(id, trans);

            Debug.Assert(current.ComputeLayerBrainLink != null);
            Debug.Assert(current.OnlineInboundAdapterLink != null);

            // update color
            if (!current.Color.Equals(color))
            {
                using var commandColor = new NpgsqlCommand(@"INSERT INTO layer_color (layer_id, color, ""timestamp"")
                    VALUES (@layer_id, @color, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandColor.Parameters.AddWithValue("layer_id", id);
                commandColor.Parameters.AddWithValue("color", color.ToArgb());
                commandColor.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandColor.ExecuteNonQueryAsync();
                current = Layer.Build(current.Name, current.ID, color, current.State, current.ComputeLayerBrainLink, current.OnlineInboundAdapterLink);
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
                current = Layer.Build(current.Name, current.ID, current.Color, state, current.ComputeLayerBrainLink, current.OnlineInboundAdapterLink);
            }

            // update clb
            if (!current.ComputeLayerBrainLink.Equals(computeLayerBrain))
            {
                using var commandCLB = new NpgsqlCommand(@"INSERT INTO layer_computelayerbrain (layer_id, brainname, ""timestamp"")
                    VALUES (@layer_id, @brainname, @timestamp)", trans.DBConnection, trans.DBTransaction);
                commandCLB.Parameters.AddWithValue("layer_id", id);
                commandCLB.Parameters.AddWithValue("brainname", computeLayerBrain.Name);
                commandCLB.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandCLB.ExecuteNonQueryAsync();
                current = Layer.Build(current.Name, current.ID, current.Color, current.State, computeLayerBrain, current.OnlineInboundAdapterLink);
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
                current = Layer.Build(current.Name, current.ID, current.Color, current.State, current.ComputeLayerBrainLink, oilp);
            }

            return current;
        }

        public async Task<bool> TryToDelete(long id, IModelContext trans)
        {
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

        // TODO: performance improvements(?)
        public async Task<LayerSet> BuildLayerSet(string[] layerNames, IModelContext trans)
        {
            var layerIDs = new List<long>();
            foreach (var ln in layerNames)
            {
                using var command = new NpgsqlCommand(@"select id from layer where name = @name LIMIT 1", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("name", ln);
                var s = await command.ExecuteScalarAsync();
                if (s == null)
                    throw new Exception(@$"Could not find layer with name ""{ln}""");
                layerIDs.Add((long)s);
            }
            return new LayerSet(layerIDs.ToArray());
        }

        public async Task<LayerSet> BuildLayerSet(IModelContext trans)
        {
            var layerIDs = new List<long>();
            using var command = new NpgsqlCommand(@"select id from layer", trans.DBConnection, trans.DBTransaction);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                layerIDs.Add(reader.GetInt64(0));
            }
            return new LayerSet(layerIDs.ToArray());
        }

        private async Task<IEnumerable<Layer>> _GetLayers(string whereClause, Action<NpgsqlParameterCollection> addParameters, IModelContext trans)
        {
            var layers = new List<Layer>();
            using var command = new NpgsqlCommand($@"SELECT l.id, l.name, ls.state, lclb.brainname, loilp.pluginname, lc.color FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC) ls
                    ON ls.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, brainname FROM layer_computelayerbrain ORDER BY layer_id, timestamp DESC) lclb
                    ON lclb.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, pluginname FROM layer_onlineinboundlayerplugin ORDER BY layer_id, timestamp DESC) loilp
                    ON loilp.layer_id = l.id
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, color FROM layer_color ORDER BY layer_id, timestamp DESC) lc
                    ON lc.layer_id = l.id
                WHERE {whereClause}", trans.DBConnection, trans.DBTransaction);
            addParameters(command.Parameters);
            using var r = await command.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetInt64(0);
                var name = r.GetString(1);
                var state = (r.IsDBNull(2)) ? DefaultState : r.GetFieldValue<AnchorState>(2);
                var clb = (r.IsDBNull(3)) ? DefaultCLB : ComputeLayerBrainLink.Build(r.GetString(3));
                var oilp = (r.IsDBNull(4)) ? DefaultOILP : OnlineInboundAdapterLink.Build(r.GetString(4));
                var color = (r.IsDBNull(5)) ? DefaultColor : Color.FromArgb(r.GetInt32(5));
                layers.Add(Layer.Build(name, id, color, state, clb, oilp));
            }
            return layers;
        }

        public async Task<Layer> GetLayer(string layerName, IModelContext trans)
        {
            var layers = await _GetLayers("l.name = @name LIMIT 1", (p) => p.AddWithValue("name", layerName), trans);
            return layers.FirstOrDefault();
        }

        public async Task<Layer> GetLayer(long layerID, IModelContext trans)
        {
            var layers = await _GetLayers("l.id = @id LIMIT 1", (p) => p.AddWithValue("id", layerID), trans);
            return layers.FirstOrDefault();
        }


        public async Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, IModelContext trans)
        {
            if (layerIDs.Length == 0) return new List<Layer>();

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
