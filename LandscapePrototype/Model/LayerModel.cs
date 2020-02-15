﻿using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class LayerModel
    {
        private readonly NpgsqlConnection conn;

        public LayerModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<long> CreateLayer(string name)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO layer (name) VALUES (@name) returning id", conn);
            command.Parameters.AddWithValue("name", name);
            var id = (long)await command.ExecuteScalarAsync();
            return id;
        }

        // TODO: performance improvements(?)
        public async Task<LayerSet> BuildLayerSet(string[] layerNames)
        {
            var layerIDs = new List<long>();
            foreach(var ln in layerNames)
            {
                using var command = new NpgsqlCommand(@"select id from layer where name = @name LIMIT 1", conn);
                command.Parameters.AddWithValue("name", ln);
                var s = await command.ExecuteScalarAsync();
                layerIDs.Add((long)s);
            }
            return new LayerSet(layerIDs.ToArray());
        }
    }
}
