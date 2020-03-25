using Landscape.Base.Model;
using LandscapePrototype.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class LayerModel : ILayerModel
    {
        private readonly NpgsqlConnection conn;

        public LayerModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<long> CreateLayer(string name, NpgsqlTransaction trans)
        {
            return await CreateLayer(name, null, trans);
        }
        public async Task<long> CreateLayer(string name, string computeLayerBrain, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO layer (name, computeLayerBrain) VALUES (@name, @computeLayerBrain) returning id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            if (computeLayerBrain == null)
                command.Parameters.AddWithValue("computeLayerBrain", DBNull.Value);
            else
                command.Parameters.AddWithValue("computeLayerBrain", computeLayerBrain);
            var id = (long)await command.ExecuteScalarAsync();
            return id;
        }

        // TODO: performance improvements(?)
        public async Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans)
        {
            var layerIDs = new List<long>();
            foreach(var ln in layerNames)
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
            while(await reader.ReadAsync())
            {
                layerIDs.Add(reader.GetInt64(0));
            }
            return new LayerSet(layerIDs.ToArray());
        }

        public async Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"select name from layer where id = @id LIMIT 1", conn, trans);
            command.Parameters.AddWithValue("id", layerID);
            using var r = await command.ExecuteReaderAsync();
            await r.ReadAsync();
            var name = r.GetString(0);
            return Layer.Build(name, layerID);
        }


        public async Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans)
        {
            if (layerIDs.Length == 0) return new List<Layer>();

            var layers = new List<Layer>();
            using var command = new NpgsqlCommand(@"select id, name from layer where id = ANY(@layer_ids)", conn, trans);
            command.Parameters.AddWithValue("layer_ids", layerIDs);
            using var r = await command.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetInt64(0);
                var name = r.GetString(1);
                layers.Add(Layer.Build(name, id));
            }

            // HACK: wonky re-sorting of layers according to input layerIDs
            return layerIDs.Select(id => layers.Find(l => l.ID == id));
        }

        public async Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans)
        {
            var layers = new List<Layer>();
            using var command = new NpgsqlCommand(@"select id, name from layer", conn, trans);
            using var r = await command.ExecuteReaderAsync();
            while(await r.ReadAsync())
            {
                var id = r.GetInt64(0);
                var name = r.GetString(1);
                layers.Add(Layer.Build(name, id));
            }
            return layers;
        }
    }
}
