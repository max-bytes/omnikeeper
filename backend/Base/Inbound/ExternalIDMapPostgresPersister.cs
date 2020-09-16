using Landscape.Base.Utils;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public class ExternalIDMapPostgresPersister : IExternalIDMapPersister
    {
        private readonly IConfiguration configuration;
        private readonly DBConnectionBuilder cb;

        private readonly static string SchemaName = "ext_id_mapping";

        public ExternalIDMapPostgresPersister(IConfiguration configuration, DBConnectionBuilder cb)
        {
            this.configuration = configuration;
            this.cb = cb;
        }
        public async Task<IDictionary<Guid, string>> Load(string scope)
        {
            var tableName = $"{scope}";
            var fullTableName = $"\"{SchemaName}\".{tableName}";
            var conn = cb.Build(configuration); // TODO: not a fan of creating our own db connection here, but the persister is instanced as a singleton and passing a connection is cumbersome

            if (conn == null) return ImmutableDictionary<Guid, string>.Empty;

            var ret = new Dictionary<Guid, string>();

            using (var trans = conn.BeginTransaction())
            {
                CreateTableIfNotExists(tableName, trans, conn);

                using (var command = new NpgsqlCommand(@$"SELECT ci_id, external_id from {fullTableName}", conn, trans))
                {
                    using var dr = await command.ExecuteReaderAsync();

                    while (await dr.ReadAsync())
                    {
                        var ciid = dr.GetGuid(0);
                        var external_id = dr.GetString(1);

                        ret.Add(ciid, external_id);
                    }
                }

                trans.Commit();
            }

            conn.Close();

            return ret;
        }

        private void CreateTableIfNotExists(string tableName, NpgsqlTransaction trans, NpgsqlConnection conn)
        {
            var fullTableName = $"\"{SchemaName}\".{tableName}";
            var createTableSQL = $@"CREATE TABLE IF NOT EXISTS {fullTableName}
                (
                    ci_id uuid NOT NULL,
                    external_id text NOT NULL,
                    CONSTRAINT {tableName}_pkey PRIMARY KEY(ci_id),
                    CONSTRAINT {tableName}_ci_id_key UNIQUE (ci_id),
                    CONSTRAINT {tableName}_external_id_key UNIQUE (external_id),
                    CONSTRAINT {tableName}_ci_id_fkey FOREIGN KEY (ci_id) REFERENCES public.ci (id) ON UPDATE RESTRICT ON DELETE RESTRICT
                )";
            using var cmdCreateTable = new NpgsqlCommand(createTableSQL, conn, trans);
            cmdCreateTable.ExecuteNonQuery();
        }

        public async Task Persist(string scope, IDictionary<Guid, string> int2ext, NpgsqlTransaction trans)
        {
            var tableName = $"{scope}";
            var fullTableName = $"\"{SchemaName}\".{tableName}";

            var conn = trans.Connection;

            CreateTableIfNotExists(tableName, trans, conn);

            // truncate 
            // TODO: find a better way to update the mappings instead of trunating and writing everything anew
            // maybe the int2ext dictionary needs to be expanded upon, with a proper way to discern old, new and deleted mappings
            using var cmdTruncate = new NpgsqlCommand($"TRUNCATE TABLE {fullTableName}", conn, trans);
            cmdTruncate.ExecuteNonQuery();

            // TODO: performance improvements -> use COPY feature of postgres
            foreach (var kv in int2ext)
            {
                using var command = new NpgsqlCommand(@$"INSERT INTO {fullTableName} (ci_id, external_id) 
                    VALUES (@ci_id, @external_id)", conn, trans);

                command.Parameters.AddWithValue("ci_id", kv.Key);
                command.Parameters.AddWithValue("external_id", kv.Value);

                await command.ExecuteScalarAsync();
            }
        }
    }

}
