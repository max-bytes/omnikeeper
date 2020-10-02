using Landscape.Base.Utils;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    // TODO: consider reworking this into using a single table, not one per scope
    public class ExternalIDMapPostgresPersister : IExternalIDMapPersister
    {
        private readonly static string SchemaName = "ext_id_mapping";

        public async Task<IDictionary<Guid, string>> Load(string scope, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            var tableName = $"{scope}"; // TODO: validate table name/scope
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
            var tableName = $"{scope}"; // TODO: validate table name/scope
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

        public IScopedExternalIDMapPersister CreateScopedPersister(string scope)
        {
            return new ScopedExternalIDMapPostgresPersister(scope, this);
        }

        private async Task<ISet<string>> GetAllTableNames(NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            var allTablesQuery = $"SELECT tablename FROM pg_tables WHERE schemaname = '{SchemaName}'";
            var allTableNames = new HashSet<string>();
            using (var command = new NpgsqlCommand(allTablesQuery, conn, trans))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var tableName = dr.GetString(0);
                    allTableNames.Add(tableName);
                }
            }
            return allTableNames;
        }

        public async Task<int> DeleteUnusedScopes(ISet<string> usedScopes, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            var allTableNames = await GetAllTableNames(conn, trans);

            var unusedScopes = allTableNames.Except(usedScopes);

            var numDeleted = 0;
            foreach(var unusedScope in unusedScopes)
            {
                var fullTableName = $"\"{SchemaName}\".{unusedScope}";
                using var command = new NpgsqlCommand($"DROP TABLE {fullTableName}", conn, trans);
                command.ExecuteNonQuery();
                numDeleted++;
            }

            return numDeleted;
        }

        public async Task<ISet<Guid>> GetAllMappedCIIDs(NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            var ret = new HashSet<Guid>();
            var allTableNames = await GetAllTableNames(conn, trans);
            foreach(var tableName in allTableNames)
            {
                var fullTableName = $"\"{SchemaName}\".{tableName}";
                using var command = new NpgsqlCommand(@$"SELECT ci_id from {fullTableName}", conn, trans);
                using var dr = await command.ExecuteReaderAsync();
                while (await dr.ReadAsync())
                {
                    var ciid = dr.GetGuid(0);

                    ret.Add(ciid);
                }
            }
            return ret;
        }
    }


    public class ScopedExternalIDMapPostgresPersister : IScopedExternalIDMapPersister
    {
        private readonly IExternalIDMapPersister centralPersister;

        public string Scope { get; }

        public ScopedExternalIDMapPostgresPersister(string scope, IExternalIDMapPersister centralPersister)
        {
            Scope = scope;
            this.centralPersister = centralPersister;
        }
        public async Task<IDictionary<Guid, string>> Load(NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            return await centralPersister.Load(Scope, conn, trans);
        }

        public async Task Persist(IDictionary<Guid, string> int2ext, NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            await centralPersister.Persist(Scope, int2ext, conn, trans);
        }
    }

}
