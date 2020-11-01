using Newtonsoft.Json;
using Npgsql;
using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
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

        public async Task<bool> AddContext(string name, GridViewConfiguration configuration)
        {
            using var command = new NpgsqlCommand($@"
                    INSERT INTO gridview_config
                    (config, name, timestamp)
                    VALUES
                    (CAST(@config AS json), @name, @timestamp)
                ", conn, null);

            var config = JsonConvert.SerializeObject(configuration);

            command.Parameters.AddWithValue("config", config);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<bool> EditContext(string name, GridViewConfiguration configuration)
        {
            using var command = new NpgsqlCommand($@"
                    UPDATE gridview_config
                    SET config = CAST(@config AS json)
                    WHERE name = @name
                ", conn, null);

            var config = JsonConvert.SerializeObject(configuration);

            command.Parameters.AddWithValue("config", config);
            command.Parameters.AddWithValue("name", name);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<bool> DeleteContext(string name)
        {
            using var command = new NpgsqlCommand($@"
                    DELETE FROM gridview_config
                    WHERE name = @name
                ", conn, null);

            command.Parameters.AddWithValue("name", name);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }
    }
}
