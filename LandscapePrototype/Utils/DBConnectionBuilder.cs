using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Utils
{
    public class DBConnectionBuilder
    {
        public NpgsqlConnection Build(string dbName)
        {
            NpgsqlConnection conn = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={dbName};");
            conn.Open();
            conn.TypeMapper.MapEnum<AttributeState>("attributestate");
            return conn;
        }
    }
}
