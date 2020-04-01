using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace LandscapeRegistry.Utils
{
    public class DBConnectionBuilder
    {
        public NpgsqlConnection Build(IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("LandscapeDatabaseConnection");
            NpgsqlConnection conn = new NpgsqlConnection(cs);
            conn.Open();
            conn.TypeMapper.MapEnum<AttributeState>("attributestate");
            conn.TypeMapper.MapEnum<RelationState>("relationstate");
            conn.TypeMapper.MapEnum<AttributeValueType>("attributevaluetype");
            conn.TypeMapper.MapEnum<UserType>("usertype");
            return conn;
        }

        public NpgsqlConnection Build(string dbName, bool pooling = true, bool reloadTypes = false)
        {
            NpgsqlConnection conn = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={dbName};Pooling={pooling}");
            conn.Open();
            if (reloadTypes) conn.ReloadTypes(); // HACK, see https://github.com/npgsql/npgsql/issues/2366
            conn.TypeMapper.MapEnum<AttributeState>("attributestate");
            conn.TypeMapper.MapEnum<RelationState>("relationstate");
            conn.TypeMapper.MapEnum<AttributeValueType>("attributevaluetype");
            conn.TypeMapper.MapEnum<UserType>("usertype");
            return conn;
        }
    }
}
