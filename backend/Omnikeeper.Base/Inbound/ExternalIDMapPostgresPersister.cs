using Npgsql;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    // TODO: consider reworking this into using a single table, not one per scope
    public class ExternalIDMapPostgresPersister : IExternalIDMapPersister
    {
        private readonly static string SchemaName = "ext_id_mapping";

        public async Task<IDictionary<Guid, string>?> Load(string scope, IModelContext trans)
        {
            try
            {
                var tableName = $"{scope}"; // TODO: validate table name/scope
                var fullTableName = $"\"{SchemaName}\".{tableName}";

                var ret = new Dictionary<Guid, string>();

                CreateTableIfNotExists(tableName, trans);

                using (var command = new NpgsqlCommand(@$"SELECT ci_id, external_id from {fullTableName}", trans.DBConnection, trans.DBTransaction))
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
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> Persist(string scope, IDictionary<Guid, string> int2ext, IModelContext trans)
        {
            try
            {
                var tableName = $"{scope}"; // TODO: validate table name/scope
                var fullTableName = $"\"{SchemaName}\".{tableName}";

                CreateTableIfNotExists(tableName, trans);

                // truncate 
                // TODO: find a better way to update the mappings instead of trunating and writing everything anew
                // maybe the int2ext dictionary needs to be expanded upon, with a proper way to discern old, new and deleted mappings
                using var cmdTruncate = new NpgsqlCommand($"TRUNCATE TABLE {fullTableName}", trans.DBConnection, trans.DBTransaction);
                cmdTruncate.ExecuteNonQuery();

                // TODO: performance improvements -> use COPY feature of postgres
                foreach (var kv in int2ext)
                {
                    using var command = new NpgsqlCommand(@$"INSERT INTO {fullTableName} (ci_id, external_id) 
                        VALUES (@ci_id, @external_id)", trans.DBConnection, trans.DBTransaction);

                    command.Parameters.AddWithValue("ci_id", kv.Key);
                    command.Parameters.AddWithValue("external_id", kv.Value);

                    await command.ExecuteScalarAsync();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public IScopedExternalIDMapPersister CreateScopedPersister(string scope)
        {
            return new ScopedExternalIDMapPostgresPersister(scope, this);
        }

        public async Task<int> DeleteUnusedScopes(ISet<string> usedScopes, IModelContext trans)
        {
            var allTableNames = await GetAllTableNames(trans);

            var unusedScopes = allTableNames.Except(usedScopes);

            var numDeleted = 0;
            foreach (var unusedScope in unusedScopes)
            {
                var fullTableName = $"\"{SchemaName}\".{unusedScope}";
                using var command = new NpgsqlCommand($"DROP TABLE {fullTableName}", trans.DBConnection, trans.DBTransaction);
                command.ExecuteNonQuery();
                numDeleted++;
            }

            return numDeleted;
        }

        public async Task<ISet<Guid>> GetAllMappedCIIDs(IModelContext trans)
        {
            var ret = new HashSet<Guid>();
            var allTableNames = await GetAllTableNames(trans);
            foreach (var tableName in allTableNames)
            {
                var fullTableName = $"\"{SchemaName}\".{tableName}";
                using var command = new NpgsqlCommand(@$"SELECT ci_id from {fullTableName}", trans.DBConnection, trans.DBTransaction);
                using var dr = await command.ExecuteReaderAsync();
                while (await dr.ReadAsync())
                {
                    var ciid = dr.GetGuid(0);

                    ret.Add(ciid);
                }
            }
            return ret;
        }

        private void CreateTableIfNotExists(string tableName, IModelContext trans)
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
            using var cmdCreateTable = new NpgsqlCommand(createTableSQL, trans.DBConnection, trans.DBTransaction);
            cmdCreateTable.ExecuteNonQuery();
        }

        private async Task<ISet<string>> GetAllTableNames(IModelContext trans)
        {
            var allTablesQuery = $"SELECT tablename FROM pg_tables WHERE schemaname = '{SchemaName}'";
            var allTableNames = new HashSet<string>();
            using (var command = new NpgsqlCommand(allTablesQuery, trans.DBConnection, trans.DBTransaction))
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
        public async Task<IDictionary<Guid, string>?> Load(IModelContext trans)
        {
            return await centralPersister.Load(Scope, trans);
        }

        public async Task<bool> Persist(IDictionary<Guid, string> int2ext, IModelContext trans)
        {
            return await centralPersister.Persist(Scope, int2ext, trans);
        }
    }
}
