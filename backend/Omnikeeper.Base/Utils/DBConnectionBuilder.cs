using Microsoft.Extensions.Configuration;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Omnikeeper.Base.Utils
{
    // the purpose of this class is to wrap NpgsqlConnection and provide an entry point GetOpenedConnection() that lazily performs some init logic on a connection
    public class NpgsqlConnectionWrapper : IDisposable
    {
        private readonly NpgsqlConnection conn;
        private readonly bool reloadTypes;
        private bool opened;

        // TODO: this grows forever... find a way to limit this!
        private static readonly ConcurrentDictionary<int, byte> connectorIDs = new ConcurrentDictionary<int, byte>();

        public NpgsqlConnectionWrapper(NpgsqlConnection conn, bool reloadTypes)
        {
            this.conn = conn;
            this.reloadTypes = reloadTypes;
            this.opened = false;
        }

        public NpgsqlConnection GetOpenedConnection()
        {
            if (!opened)
            {
                conn.Open();
                connectorIDs[conn.ProcessID] = 1;
                if (reloadTypes) conn.ReloadTypes(); // HACK, see https://github.com/npgsql/npgsql/issues/2366
                MapEnums(conn);
                opened = true;
            }
            return conn;
        }

        private void MapEnums(NpgsqlConnection conn)
        {
            conn.TypeMapper.MapEnum<AnchorState>("anchorstate");
            conn.TypeMapper.MapEnum<AttributeValueType>("attributevaluetype");
            conn.TypeMapper.MapEnum<UserType>("usertype");
            conn.TypeMapper.MapEnum<DataOriginType>("dataorigintype");
            conn.TypeMapper.MapEnum<UsageStatsOperation>("usagestatsoperation");
        }

        public static bool HasConnectorID(int id) => connectorIDs.ContainsKey(id);

        public void Dispose()
        {
            conn.Dispose();
        }
    }
    public class DBConnectionBuilder
    {
        public NpgsqlConnectionWrapper BuildFromConnectionString(string cs, bool reloadTypes)
        {
            NpgsqlConnection conn = new NpgsqlConnection(cs);
            return new NpgsqlConnectionWrapper(conn, reloadTypes);
        }

        public NpgsqlConnectionWrapper BuildFromUserSecrets(Assembly rootAssembly, bool reloadTypes, string configName = "DatabaseConnection")
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

    }
}
