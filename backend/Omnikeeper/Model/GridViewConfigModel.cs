using Newtonsoft.Json;
using Npgsql;
using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class GridViewConfigModel : IGridViewConfigModel
    {
        private readonly NpgsqlConnection conn;
        public GridViewConfigModel(NpgsqlConnection connection)
        {
            conn = connection;
        }
        public async Task<GridViewConfiguration> GetConfiguration(string configName)
        {
            using var command = new NpgsqlCommand($@"
                    SELECT *
                    FROM gridview_config gvc
                    WHERE gvc.name=@configName
                ", conn, null);

            command.Parameters.AddWithValue("configName", configName);

            using var dr = await command.ExecuteReaderAsync();

            string configJson = "", name;

            while (dr.Read())
            {
                configJson = dr.GetString(1);
                name = dr.GetString(2);
            }

            var config = JsonConvert.DeserializeObject<GridViewConfiguration>(configJson);

            return config;
        }
    }
}
