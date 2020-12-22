using Microsoft.Extensions.Configuration;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

namespace Omnikeeper.Base.Utils
{
    public class DBConnectionBuilder
    {
        private readonly ISet<int> connectorIDs = new HashSet<int>();

        public NpgsqlConnection Build(IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("LandscapeDatabaseConnection"); // TODO: add Enlist=false to connection string
            NpgsqlConnection conn = new NpgsqlConnection(cs);
            conn.Open();
            connectorIDs.Add(conn.ProcessID);
            conn.TypeMapper.UseJsonNet();
            MapEnums(conn);
            return conn;
        }

        public bool HasConnectorID(int id) => connectorIDs.Contains(id);

        public NpgsqlConnection Build(string dbName, bool pooling = true, bool reloadTypes = false)
        {
            NpgsqlConnection conn = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={dbName};Pooling={pooling};Enlist=false");
            conn.Open();
            if (reloadTypes) conn.ReloadTypes(); // HACK, see https://github.com/npgsql/npgsql/issues/2366
            conn.TypeMapper.UseJsonNet();
            MapEnums(conn);
            return conn;
        }

        private void MapEnums(NpgsqlConnection conn)
        {
            conn.TypeMapper.MapEnum<AttributeState>("attributestate");
            conn.TypeMapper.MapEnum<RelationState>("relationstate");
            conn.TypeMapper.MapEnum<AnchorState>("anchorstate");
            conn.TypeMapper.MapEnum<AttributeValueType>("attributevaluetype");
            conn.TypeMapper.MapEnum<UserType>("usertype");
            conn.TypeMapper.MapEnum<DataOriginType>("dataorigintype");
        }
    }
}
