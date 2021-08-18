using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Model
{
    public class GridViewContextModel : IGridViewContextModel
    {
        public async Task<List<Context>> GetContexts(IModelContext trans)
        {
            var contexts = new List<Context>();

            using var command = new NpgsqlCommand($@"
                    SELECT name, speaking_name, description
                    FROM config.gridview
                ", trans.DBConnection, trans.DBTransaction);

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                contexts.Add(new Context(dr.GetString(0), dr.GetString(1), dr.GetString(2)));
            }

            return contexts;
        }

        public async Task<bool> AddContext(string name, string speakingName, string description, GridViewConfiguration configuration, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    INSERT INTO config.gridview
                    (config, name, timestamp, speaking_name, description)
                    VALUES
                    (@config, @name, @timestamp, @speaking_name, @description)
                ", trans.DBConnection, trans.DBTransaction);

            var config = JsonConvert.SerializeObject(configuration);

            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = config });
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
                    UPDATE config.gridview
                    SET 
                        name = @name,
                        speaking_name = @speaking_name,
                        description = @description,
                        config = @config
                    WHERE name = @name
                ", trans.DBConnection, trans.DBTransaction);

            var config = JsonConvert.SerializeObject(configuration);

            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = config });
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("speaking_name", speakingName);
            command.Parameters.AddWithValue("description", description);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> DeleteContext(string name, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    DELETE FROM config.gridview
                    WHERE name = @name
                ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("name", name);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<FullContext> GetFullContextByName(string contextName, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                    SELECT name, speaking_name, description, config
                    FROM config.gridview
                    WHERE name = @name
                    LIMIT 1
                ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("name", contextName);

            using var dr = await command.ExecuteReaderAsync();

            if (!dr.Read())
                throw new Exception($"Could not find context named \"{contextName}\"");

            var name = dr.GetString(0);
            var speakingName = dr.GetString(1);
            var description = dr.GetString(2);
            var configJson = dr.GetString(3);
            var config = JsonConvert.DeserializeObject<GridViewConfiguration>(configJson);

            return new FullContext(name, speakingName, description, config);
        }
    }
}
