using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class LayerModel : ILayerModel
    {
        private readonly NpgsqlConnection conn;

        private static readonly AnchorState DefaultState = AnchorState.Active;

        public LayerModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<Layer> CreateLayer(string name, NpgsqlTransaction trans)
        {
            return await CreateLayer(name, DefaultState, null, trans);
        }
        public async Task<Layer> CreateLayer(string name, AnchorState state, string computeLayerBrain, NpgsqlTransaction trans)
        {
            // TODO: make computelayerbrain its own table
            using var command = new NpgsqlCommand(@"INSERT INTO layer (name, computeLayerBrain) VALUES (@name, @computeLayerBrain) returning id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            if (computeLayerBrain == null)
                command.Parameters.AddWithValue("computeLayerBrain", DBNull.Value);
            else
                command.Parameters.AddWithValue("computeLayerBrain", computeLayerBrain);
            var id = (long)await command.ExecuteScalarAsync();

            // set state
            using var commandState = new NpgsqlCommand(@"INSERT INTO layer_state (layer_id, state, ""timestamp"")
                    VALUES (@layer_id, @state, now())", conn, trans);
            commandState.Parameters.AddWithValue("layer_id", id);
            commandState.Parameters.AddWithValue("state", state);
            await commandState.ExecuteNonQueryAsync();

            return Layer.Build(name, id, state);
        }

        public async Task<Layer> Update(long id, AnchorState state, NpgsqlTransaction trans)
        {
            var current = await GetLayer(id, trans);

            // update state
            if (current.State != state)
            {
                using var commandState = new NpgsqlCommand(@"INSERT INTO layer_state (layer_id, state, ""timestamp"")
                    VALUES (@layer_id, @state, now())", conn, trans);
                commandState.Parameters.AddWithValue("layer_id", id);
                commandState.Parameters.AddWithValue("state", state);
                await commandState.ExecuteNonQueryAsync();
                current = Layer.Build(current.Name, current.ID, state);
            }

            return current;
        }

        public async Task<bool> TryToDelete(long id, NpgsqlTransaction trans)
        {
            try
            {
                using var command = new NpgsqlCommand(@"DELETE FROM layer WHERE id = @id", conn, trans);
                command.Parameters.AddWithValue("id", id);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (PostgresException e)
            {
                return false;
            }
        }

        // TODO: performance improvements(?)
        public async Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans)
        {
            var layerIDs = new List<long>();
            foreach (var ln in layerNames)
            {
                using var command = new NpgsqlCommand(@"select id from layer where name = @name LIMIT 1", conn, trans);
                command.Parameters.AddWithValue("name", ln);
                var s = await command.ExecuteScalarAsync();
                if (s == null)
                    throw new Exception(@$"Could not find layer with name ""{ln}""");
                layerIDs.Add((long)s);
            }
            return new LayerSet(layerIDs.ToArray());
        }

        public async Task<LayerSet> BuildLayerSet(NpgsqlTransaction trans)
        {
            var layerIDs = new List<long>();
            using var command = new NpgsqlCommand(@"select id from layer", conn, trans);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                layerIDs.Add(reader.GetInt64(0));
            }
            return new LayerSet(layerIDs.ToArray());
        }

        public async Task<Layer> GetLayer(string layerName, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT l.id, ls.state FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC) ls
                    ON ls.layer_id = l.id
                WHERE l.name = @name LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("name", layerName);
            using var r = await command.ExecuteReaderAsync();
            await r.ReadAsync();
            if (!r.HasRows) return null;
            var id = r.GetInt64(0);
            var state = (r.IsDBNull(1)) ? DefaultState : r.GetFieldValue<AnchorState>(1);
            return Layer.Build(layerName, id, state);
        }

        public async Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT l.name, ls.state FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC) ls
                    ON ls.layer_id = l.id
                WHERE l.id = @id LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("id", layerID);
            using var r = await command.ExecuteReaderAsync();
            await r.ReadAsync();
            if (!r.HasRows) return null;
            var name = r.GetString(0);
            var state = (r.IsDBNull(1)) ? DefaultState : r.GetFieldValue<AnchorState>(1);
            return Layer.Build(name, layerID, state);
        }


        public async Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans)
        {
            if (layerIDs.Length == 0) return new List<Layer>();

            var layers = new List<Layer>();
            using var command = new NpgsqlCommand(@"SELECT l.id, l.name, ls.state FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC) ls
                    ON ls.layer_id = l.id
                WHERE l.id = ANY(@layer_ids)", conn, trans);
            command.Parameters.AddWithValue("layer_ids", layerIDs);
            using var r = await command.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetInt64(0);
                var name = r.GetString(1);
                var state = (r.IsDBNull(2)) ? DefaultState : r.GetFieldValue<AnchorState>(2);
                layers.Add(Layer.Build(name, id, state));
            }

            // HACK: wonky re-sorting of layers according to input layerIDs
            return layerIDs.Select(id => layers.Find(l => l.ID == id));
        }

        public async Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans)
        {
            var layers = new List<Layer>();
            using var command = new NpgsqlCommand(@"SELECT l.id, l.name, ls.state FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC) ls
                    ON ls.layer_id = l.id", conn, trans);
            using var r = await command.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetInt64(0);
                var name = r.GetString(1);
                var state = (r.IsDBNull(2)) ? DefaultState : r.GetFieldValue<AnchorState>(2);
                layers.Add(Layer.Build(name, id, state));
            }
            return layers;
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, NpgsqlTransaction trans)
        {
            var layers = new List<Layer>();
            using var command = new NpgsqlCommand(@"SELECT l.id, l.name, ls.state FROM layer l
                LEFT JOIN 
                    (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state ORDER BY layer_id, timestamp DESC) ls
                    ON ls.layer_id = l.id
                WHERE ls.state = ANY(@states) OR ls.state IS NULL", conn, trans);
            command.Parameters.AddWithValue("states", stateFilter.Filter2States());
            using var r = await command.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetInt64(0);
                var name = r.GetString(1);
                var state = (r.IsDBNull(2)) ? DefaultState : r.GetFieldValue<AnchorState>(2);
                layers.Add(Layer.Build(name, id, state));
            }
            return layers;
        }
    }
}
