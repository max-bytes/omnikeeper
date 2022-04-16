using Microsoft.Extensions.Configuration;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Concurrent;
using System.Reflection;

namespace Omnikeeper.Base.Utils
{
    public class DBConnectionBuilder
    {
        // TODO: this grows forever... find a way to limit this!
        private readonly ConcurrentDictionary<int, byte> connectorIDs = new ConcurrentDictionary<int, byte>();

        public NpgsqlConnection BuildFromConnectionString(string cs, bool reloadTypes)
        {
            NpgsqlConnection conn = new NpgsqlConnection(cs);
            conn.Open();
            connectorIDs[conn.ProcessID] = 1;
            if (reloadTypes) conn.ReloadTypes(); // HACK, see https://github.com/npgsql/npgsql/issues/2366
            //conn.TypeMapper.UseJsonNet(); // TODO: remove
            MapEnums(conn);
            return conn;
        }

        public NpgsqlConnection BuildFromUserSecrets(Assembly rootAssembly, bool reloadTypes, string configName = "DatabaseConnection")
        {
            var cs = GetConnectionStringFromUserSecrets(rootAssembly, configName);
            return BuildFromConnectionString(cs, reloadTypes);
        }

        public static string GetConnectionStringFromUserSecrets(Assembly rootAssembly, string configName = "DatabaseConnection")
        {
            ConfigurationBuilder cb = new ConfigurationBuilder();
            cb.AddUserSecrets(rootAssembly);
            cb.AddEnvironmentVariables();
            var c = cb.Build();
            var connectionString = c.GetConnectionString(configName);
            return connectionString;
        }

        public bool HasConnectorID(int id) => connectorIDs.ContainsKey(id);

        //public NpgsqlConnection Build(string dbName)
        //{
        //    bool pooling = false;
        //    bool reloadTypes = true;
        //    NpgsqlConnection conn = new NpgsqlConnection($"Server=localhost;Port=15432;User Id=postgres; Password=postgres;Database={dbName};Pooling={pooling};Maximum Pool Size=40;Enlist=false");
        //    conn.Open();
        //    connectorIDs.Add(conn.ProcessID);
        //    if (reloadTypes) conn.ReloadTypes(); // HACK, see https://github.com/npgsql/npgsql/issues/2366
        //    conn.TypeMapper.UseJsonNet();
        //    MapEnums(conn);
        //    return conn;
        //}

        private void MapEnums(NpgsqlConnection conn)
        {
            conn.TypeMapper.MapEnum<AnchorState>("anchorstate");
            conn.TypeMapper.MapEnum<AttributeValueType>("attributevaluetype");
            conn.TypeMapper.MapEnum<UserType>("usertype");
            conn.TypeMapper.MapEnum<DataOriginType>("dataorigintype");
        }
    }
}
