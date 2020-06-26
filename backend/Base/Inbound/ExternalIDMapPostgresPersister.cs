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
        private readonly string tableName;

        public ExternalIDMapPostgresPersister(IConfiguration configuration, DBConnectionBuilder cb, string tableName)
        {
            this.configuration = configuration;
            this.cb = cb;
            this.tableName = tableName;
        }
        public async Task<IDictionary<Guid, string>> Load()
        {
            var conn = cb.Build(configuration);

            if (conn == null) return ImmutableDictionary<Guid, string>.Empty;

            var ret = new Dictionary<Guid, string>();

            using (var trans = conn.BeginTransaction())
            {
                CreateTableIfNotExists(trans, conn);

                using (var command = new NpgsqlCommand(@$"SELECT ci_id, external_id from {tableName}", conn, trans))
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

        private void CreateTableIfNotExists(NpgsqlTransaction trans, NpgsqlConnection conn)
        {
            var createSQL = $@"CREATE TABLE IF NOT EXISTS public.{tableName}
                (
                    ci_id uuid NOT NULL,
                    external_id text NOT NULL,
                    CONSTRAINT {tableName}_pkey PRIMARY KEY(ci_id)
                )";
            using var cmdCreate = new NpgsqlCommand(createSQL, conn, trans);
            cmdCreate.ExecuteNonQuery();
        }

        public async Task Persist(IDictionary<Guid, string> int2ext)
        {
            var conn = cb.Build(configuration);

            if (conn == null) return;

            using var trans = conn.BeginTransaction();

            CreateTableIfNotExists(trans, conn);

            // truncate // TODO: maybe find a better way to update the mappings instead of trunating and writing everything anew
            using var cmdTruncate = new NpgsqlCommand($"TRUNCATE TABLE public.{tableName}", conn, trans);
            cmdTruncate.ExecuteNonQuery();

            // TODO: performance improvements -> use COPY feature of postgres
            foreach (var kv in int2ext) {
                using var command = new NpgsqlCommand(@$"INSERT INTO {tableName} (ci_id, external_id) 
                    VALUES (@ci_id, @external_id)", conn, trans);

                command.Parameters.AddWithValue("ci_id", kv.Key);
                command.Parameters.AddWithValue("external_id", kv.Value);

                await command.ExecuteScalarAsync();
            }

            trans.Commit();

            conn.Close();
        }
    }

}
