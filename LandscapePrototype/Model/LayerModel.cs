using Npgsql;
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

        // TODO: performance improvements(?)
        public LayerSet BuildLayerSet(string[] layerNames)
        {
            var layerIDs = layerNames.Select(ln =>
            {
                using (var command = new NpgsqlCommand(@"select id from layer where name = @name LIMIT 1", conn))
                {
                    command.Parameters.AddWithValue("name", ln);
                    var s = command.ExecuteScalar();
                    return (long)s;
                }
            });
            return new LayerSet(layerIDs.ToArray());
        }
    }
}
