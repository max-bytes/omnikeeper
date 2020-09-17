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
        private readonly static string SchemaName = "ext_id_mapping";

        public async Task<IDictionary<Guid, string>> Load(string scope, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            var tableName = $"{scope}";
            var fullTableName = $"\"{SchemaName}\".{tableName}";

            var ret = new Dictionary<Guid, string>();

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

        public async Task Persist(string scope, IDictionary<Guid, string> int2ext, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            var tableName = $"{scope}";
            var fullTableName = $"\"{SchemaName}\".{tableName}";

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

        public IScopedExternalIDMapPersister CreateScopedPersister(string scope, NpgsqlConnection conn)
        {
            return new ScopedExternalIDMapPostgresPersister(scope, this, conn);
        }
    }


    public class ScopedExternalIDMapPostgresPersister : IScopedExternalIDMapPersister
    {
        private readonly IExternalIDMapPersister centralPersister;
        private readonly NpgsqlConnection conn;

        public string Scope { get; }

        public ScopedExternalIDMapPostgresPersister(string scope, IExternalIDMapPersister centralPersister, NpgsqlConnection conn)
        {
            Scope = scope;
            this.centralPersister = centralPersister;
            this.conn = conn;
        }
        public async Task<IDictionary<Guid, string>> Load(NpgsqlTransaction trans)
        {
            return await centralPersister.Load(Scope, conn, trans);
        }

        public async Task Persist(IDictionary<Guid, string> int2ext, NpgsqlTransaction trans)
        {
            await centralPersister.Persist(Scope, int2ext, conn, trans);
        }
    }

}
