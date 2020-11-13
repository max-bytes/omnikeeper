using Newtonsoft.Json;
using Npgsql;
using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class GridViewConfigModel : IGridViewConfigModel
    {
        public async Task<GridViewConfiguration> GetConfiguration(string configName, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    SELECT *
                    FROM gridview_config gvc
                    WHERE gvc.name=@configName
                ", trans.DBConnection, trans.DBTransaction);

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

        public async Task<List<Context>> GetContexts(IModelContext trans)
        {
            var contexts = new List<Context>();

            using var command = new NpgsqlCommand($@"
                    SELECT id, name, speaking_name, description
                    FROM gridview_config
                ", trans.DBConnection, trans.DBTransaction);

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                contexts.Add(new Context
                {
                    Id = dr.GetInt32(0),
                    Name = dr.GetString(1),
                    SpeakingName = dr.GetString(2),
                    Description = dr.GetString(3)
                });
            }

            return contexts;
        }

        public async Task<bool> AddContext(string name, string speakingName, string description, GridViewConfiguration configuration, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    INSERT INTO gridview_config
                    (config, name, timestamp, speaking_name, description)
                    VALUES
                    (CAST(@config AS json), @name, @timestamp, @speaking_name, @description)
                ", trans.DBConnection, trans.DBTransaction);

            var config = JsonConvert.SerializeObject(configuration);

            command.Parameters.AddWithValue("config", config);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("speaking_name", speakingName);
            command.Parameters.AddWithValue("description", description);
            command.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<bool> EditContext(string name, string speakingName, string description, GridViewConfiguration configuration, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    UPDATE gridview_config
                    SET 
                        name = @name,
                        speaking_name = @speaking_name,
                        description = @description,
                        config = CAST(@config AS json)
                    WHERE name = @name
                ", trans.DBConnection, trans.DBTransaction);

            var config = JsonConvert.SerializeObject(configuration);

            command.Parameters.AddWithValue("config", config);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("speaking_name", speakingName);
            command.Parameters.AddWithValue("description", description);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<bool> DeleteContext(string name, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    DELETE FROM gridview_config
                    WHERE name = @name
                ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("name", name);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }
    }
}
