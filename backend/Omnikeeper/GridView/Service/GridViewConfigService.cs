using Newtonsoft.Json;
using Npgsql;
using Omnikeeper.GridView.Model;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Service
{
    public class GridViewConfigService
    {
        // get grid view config data
        // try to save config data on cache memory

        private readonly NpgsqlConnection conn;
        public GridViewConfigService(NpgsqlConnection connection)
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

            // TO DO: use System.Text.Json to deserialize objects

            //var options = new JsonSerializerOptions
            //{
            //    AllowTrailingCommas = true,
            //    IgnoreNullValues = true,
            //    WriteIndented = true
            //};

            //config = JsonSerializer.Deserialize<GridViewConfiguration>(configJson, options);

            var config = JsonConvert.DeserializeObject<GridViewConfiguration>(configJson);

            return config;
        }
    }
}
