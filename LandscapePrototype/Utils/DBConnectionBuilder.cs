using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Utils
{
    public class DBConnectionBuilder
    {
        public NpgsqlConnection Build(string dbName, bool pooling = true, bool reloadTypes = false)
        {
            NpgsqlConnection conn = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={dbName};Pooling={pooling}");
            conn.Open();
            if (reloadTypes) conn.ReloadTypes(); // HACK, see https://github.com/npgsql/npgsql/issues/2366
            conn.TypeMapper.MapEnum<AttributeState>("attributestate");
            conn.TypeMapper.MapEnum<RelationState>("relationstate");
            return conn;
        }
    }
}
