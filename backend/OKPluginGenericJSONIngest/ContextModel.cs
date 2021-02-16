using DBMigrations;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKPluginGenericJSONIngest
{
    public interface IContextModel
    {
        Task<IEnumerable<Context>> GetAllContexts(IModelContext trans);
        Task<Context?> GetContextByName(string name, IModelContext trans);
        Task<Context> Upsert(string name, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig, IModelContext trans);
        Task<Context> Delete(string name, IModelContext trans);
    }

    public class ContextModel : IContextModel
    {
        public async Task<IEnumerable<Context>> GetAllContexts(IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                SELECT name, extractConfig, transformConfig, loadConfig FROM {PluginDBMigrator.PluginDBSchemaName}.context
            ", trans.DBConnection, trans.DBTransaction);

            using var s = await command.ExecuteReaderAsync();

            var ret = new List<Context>();
            while (s.Read())
            {
                var name = s.GetString(0);
                var extractConfigJO = s.GetFieldValue<JObject>(1);
                var transformConfigJO = s.GetFieldValue<JObject>(2);
                var loadConfigJO = s.GetFieldValue<JObject>(3);
               ret.Add(Deserialize(name, extractConfigJO, transformConfigJO, loadConfigJO));
            }
            return ret;
        }

        public async Task<Context?> GetContextByName(string name, IModelContext trans)
        {
            try
            {
                using var command = new NpgsqlCommand($@"
                    SELECT extractConfig, transformConfig, loadConfig FROM {PluginDBMigrator.PluginDBSchemaName}.context WHERE name = @name LIMIT 1
                ", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("name", name);
                using var s = await command.ExecuteReaderAsync();
                if (!await s.ReadAsync())
                    throw new Exception($"Could not find context with name {name}");

                var extractConfigJO = s.GetFieldValue<JObject>(0);
                var transformConfigJO = s.GetFieldValue<JObject>(1);
                var loadConfigJO = s.GetFieldValue<JObject>(2); 
                var d = Deserialize(name, extractConfigJO, transformConfigJO, loadConfigJO);
                return d;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Context> Upsert(string name, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig, IModelContext trans)
        {
            var extractConfigJO = Context.ExtractConfigSerializer.SerializeToJObject(extractConfig);
            var transformConfigJO = Context.TransformConfigSerializer.SerializeToJObject(transformConfig);
            var loadConfigJO = Context.LoadConfigSerializer.SerializeToJObject(loadConfig);
            using var command = new NpgsqlCommand(@$"INSERT INTO {PluginDBMigrator.PluginDBSchemaName}.context (name, extractConfig, transformConfig, loadConfig) VALUES 
                (@name, @extractConfig, @transformConfig, @loadConfig) ON CONFLICT (name) DO UPDATE SET 
                extractConfig = EXCLUDED.extractConfig, transformConfig = EXCLUDED.transformConfig, loadConfig = EXCLUDED.loadConfig", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.Add(new NpgsqlParameter("extractConfig", NpgsqlDbType.Json) { Value = extractConfigJO });
            command.Parameters.Add(new NpgsqlParameter("transformConfig", NpgsqlDbType.Json) { Value = transformConfigJO });
            command.Parameters.Add(new NpgsqlParameter("loadConfig", NpgsqlDbType.Json) { Value = loadConfigJO });
            await command.ExecuteNonQueryAsync();
            var d = Deserialize(name, extractConfigJO, transformConfigJO, loadConfigJO);
            return d;
        }

        public async Task<Context> Delete(string name, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@$"DELETE FROM {PluginDBMigrator.PluginDBSchemaName}.context WHERE name = @name RETURNING extractConfig, transformConfig, loadConfig", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("name", name);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var extractConfigJO = reader.GetFieldValue<JObject>(0);
            var transformConfigJO = reader.GetFieldValue<JObject>(1);
            var loadConfigJO = reader.GetFieldValue<JObject>(2);

            var d = Deserialize(name, extractConfigJO, transformConfigJO, loadConfigJO);
            return d;
        }

        private Context Deserialize(string name, JObject extractConfigJO, JObject transformConfigJO, JObject loadConfigJO)
        {
            var extractConfig = Context.ExtractConfigSerializer.Deserialize(extractConfigJO);
            var transformConfig = Context.TransformConfigSerializer.Deserialize(transformConfigJO);
            var loadConfig = Context.LoadConfigSerializer.Deserialize(loadConfigJO);
            return new Context(name, extractConfig, transformConfig, loadConfig);
        }
    }
}
