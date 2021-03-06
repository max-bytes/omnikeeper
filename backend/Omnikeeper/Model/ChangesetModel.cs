using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Omnikeeper.Base.Model.IChangesetModel;

namespace Omnikeeper.Model
{
    public class ChangesetModel : IChangesetModel
    {
        private readonly IUserInDatabaseModel userModel;

        public ChangesetModel(IUserInDatabaseModel userModel)
        {
            this.userModel = userModel;
        }

        public async Task<Changeset> CreateChangeset(long userID, string layerID, DataOriginV1 dataOrigin, IModelContext trans, DateTimeOffset? timestamp = null)
        {
            var user = await userModel.GetUser(userID, trans);
            if (user == null)
                throw new Exception($"Could not find user with ID {userID}");
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (id, timestamp, user_id, layer_id, origin_type) VALUES (@id, @timestamp, @user_id, @layer_id, @origin_type) returning timestamp", trans.DBConnection, trans.DBTransaction);
            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("user_id", userID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("origin_type", dataOrigin.Type);
            command.Parameters.AddWithValue("timestamp", timestamp.GetValueOrDefault(DateTimeOffset.UtcNow).ToUniversalTime());
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var timestampR = reader.GetDateTime(0);
            return new Changeset(id, user, layerID, dataOrigin, timestampR);
        }

        public async Task<Changeset?> GetChangeset(Guid id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT c.timestamp, c.user_id, c.layer_id, c.origin_type, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.id = @id", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("id", id);
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var timestamp = dr.GetDateTime(0);
            var userID = dr.GetInt64(1);
            var layerID = dr.GetString(2);
            var dataOriginType = dr.GetFieldValue<DataOriginType>(3);
            var origin = new DataOriginV1(dataOriginType);
            var username = dr.GetString(4);
            var displayName = dr.GetString(5);
            var keycloakUUID = dr.GetGuid(6);
            var userType = dr.GetFieldValue<UserType>(7);
            var userTimestamp = dr.GetDateTime(8);

            var user = new UserInDatabase(userID, keycloakUUID, username, displayName, userType, userTimestamp);
            return new Changeset(id, user, layerID, origin, timestamp);
        }

        public async Task<IReadOnlyList<Changeset>> GetChangesets(ISet<Guid> ids, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"SELECT c.id, c.timestamp, c.user_id, c.layer_id, c.origin_type, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.id = ANY(@ids)
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("ids", ids.ToArray());
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetGuid(0);
                var timestamp = dr.GetDateTime(1);
                var userID = dr.GetInt64(2);
                var layerID = dr.GetString(3);
                var dataOriginType = dr.GetFieldValue<DataOriginType>(4);
                var origin = new DataOriginV1(dataOriginType);
                var username = dr.GetString(5);
                var displayName = dr.GetString(6);
                var keycloakUUID = dr.GetGuid(7);
                var userType = dr.GetFieldValue<UserType>(8);
                var userTimestamp = dr.GetDateTime(9);

                var user = new UserInDatabase(userID, keycloakUUID, username, displayName, userType, userTimestamp);
                ret.Add(new Changeset(id, user, layerID, origin, timestamp));
            }
            return ret;
        }

        public async Task<IReadOnlySet<Guid>> GetCIIDsAffectedByChangeset(Guid changesetID, IModelContext trans)
        {
            var query = @"SELECT DISTINCT inn.ci_id FROM (
                SELECT a.ci_id as ci_id FROM attribute a WHERE a.changeset_id = @changeset_id
                UNION
                SELECT r.from_ci_id as ci_id FROM relation r WHERE r.changeset_id = @changeset_id
                UNION
                SELECT r.to_ci_id as ci_id FROM relation r WHERE r.changeset_id = @changeset_id
            ) inn";
            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", changesetID);
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            var ret = new HashSet<Guid>();
            while (await dr.ReadAsync())
            {
                ret.Add(dr.GetGuid(0));
            }
            return ret;
        }

        // returns all changesets in the time range
        // sorted by timestamp
        public async Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IChangesetSelection cs, IModelContext trans, int? limit = null)
        {
            return cs switch
            {
                ChangesetSelectionSpecificCIs mci => await GetChangesetsInTimespan(from, to, layers, mci.CIIDs, trans, limit),
                ChangesetSelectionAllCIs _ => await GetChangesetsInTimespan(from, to, layers, trans, limit),
                _ => throw new Exception("Invalid changeset selection"),
            };
        }


        // returns all changesets affecting these CI, both via attributes OR relations
        // sorted by timestamp
        private async Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, Guid[] ciids, IModelContext trans, int? limit = null)
        {
            var queryAttributes = @"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                INNER JOIN attribute a ON a.changeset_id = c.id 
                INNER JOIN ci ci ON a.ci_id = ci.id
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND a.layer_id = ANY(@layer_ids)";
            queryAttributes += " AND ci.id = ANY(@ciids)";

            var irdClause = "r.from_ci_id = ci.id OR r.to_ci_id = ci.id";
            var queryRelations = $@"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                INNER JOIN relation r ON r.changeset_id = c.id 
                INNER JOIN ci ci ON ({irdClause})
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND r.layer_id = ANY(@layer_ids)";
            queryRelations += " AND ci.id = ANY(@ciids)";

            var query = @$" {queryAttributes} UNION {queryRelations} ORDER BY 5 DESC";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("from", from.ToUniversalTime());
            command.Parameters.AddWithValue("to", to.ToUniversalTime());
            command.Parameters.AddWithValue("ciids", ciids);
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            if (limit.HasValue)
                command.Parameters.AddWithValue("limit", limit.Value);
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetGuid(0);
                var userID = dr.GetInt64(1);
                var layerID = dr.GetString(2);
                var dataOriginType = dr.GetFieldValue<DataOriginType>(3);
                var origin = new DataOriginV1(dataOriginType);
                var timestamp = dr.GetDateTime(4);
                var username = dr.GetString(5);
                var displayName = dr.GetString(6);
                var userUUID = dr.GetGuid(7);
                var userType = dr.GetFieldValue<UserType>(8);
                var userTimestamp = dr.GetDateTime(9);

                var user = new UserInDatabase(userID, userUUID, username, displayName, userType, userTimestamp);
                var c = new Changeset(id, user, layerID, origin, timestamp);
                ret.Add(c);
            }
            return ret;
        }

        private async Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IModelContext trans, int? limit = null)
        {
            var query = @"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to
                AND c.layer_id = ANY(@layer_ids)
                ORDER BY c.timestamp DESC NULLS LAST";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("from", from.ToUniversalTime());
            command.Parameters.AddWithValue("to", to.ToUniversalTime());
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            if (limit.HasValue)
                command.Parameters.AddWithValue("limit", limit.Value);
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetGuid(0);
                var userID = dr.GetInt64(1);
                var layerID = dr.GetString(2);
                var dataOriginType = dr.GetFieldValue<DataOriginType>(3);
                var origin = new DataOriginV1(dataOriginType);
                var timestamp = dr.GetDateTime(4);
                var username = dr.GetString(5);
                var displayName = dr.GetString(6);
                var userUUID = dr.GetGuid(7);
                var userType = dr.GetFieldValue<UserType>(8);
                var userTimestamp = dr.GetDateTime(9);

                var user = new UserInDatabase(userID, userUUID, username, displayName, userType, userTimestamp);
                var c = new Changeset(id, user, layerID, origin, timestamp);
                ret.Add(c);
            }
            return ret;
        }

        public async Task<Changeset?> GetLatestChangesetForLayer(string layerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            using var command = new NpgsqlCommand(@"SELECT c.id, c.timestamp, c.user_id, c.origin_type, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.layer_id = @layer_id AND c.timestamp <= @threshold
                ORDER BY c.timestamp DESC
                LIMIT 1", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("threshold", timeThreshold.Time.ToUniversalTime());
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetGuid(0);
            var timestamp = dr.GetDateTime(1);
            var userID = dr.GetInt64(2);
            var dataOriginType = dr.GetFieldValue<DataOriginType>(3);
            var origin = new DataOriginV1(dataOriginType);
            var username = dr.GetString(4);
            var displayName = dr.GetString(5);
            var keycloakUUID = dr.GetGuid(6);
            var userType = dr.GetFieldValue<UserType>(7);
            var userTimestamp = dr.GetDateTime(8);

            var user = new UserInDatabase(userID, keycloakUUID, username, displayName, userType, userTimestamp);
            return new Changeset(id, user, layerID, origin, timestamp);
        }

        public async Task<int> DeleteEmptyChangesets(int limit, IModelContext trans)
        {
            // NOTE: we do it in this roundabout way because otherwise, query takes a long time and might timeout for large datasets
            var emptyChangesetsQuery = @"
                select c.id from changeset c where c.id not in (
	            select distinct changeset_id from attribute
	            union
	            select distinct changeset_id from relation
	            ) limit @limit";
            using var emptyChangesetsCommand = new NpgsqlCommand(emptyChangesetsQuery, trans.DBConnection, trans.DBTransaction);
            emptyChangesetsCommand.Parameters.AddWithValue("limit", limit);
            emptyChangesetsCommand.Prepare();
            var emptyChangesets = new List<Guid>();
            using (var dr = await emptyChangesetsCommand.ExecuteReaderAsync())
            {
                while (await dr.ReadAsync())
                    emptyChangesets.Add(dr.GetGuid(0));
            }

            var query = @"delete from changeset c where c.id = any(@empty_changesets)";
            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("empty_changesets", emptyChangesets);
            command.Prepare();

            var numArchived = await command.ExecuteNonQueryAsync();

            return numArchived;
        }

        /// <summary>
        /// approach: only archive a changeset when ALL of its changes can be archived... which means that ALL of its changes to attribute and relations can be archived
        /// this is the case when the timestamp of the attribute/relation is older than the threshold AND the attribute/relation is NOT part of the latest/current data
        /// we rely on foreign key constraints and cascading deletes to delete the corresponding attributes and relations
        /// </summary>
        /// <param name="threshold"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        [Obsolete]
        public async Task<int> ArchiveUnusedChangesetsOlderThan(DateTimeOffset threshold, IModelContext trans)
        {
            // TODO: use latest tables
            var query = @"delete from changeset where
                id NOT in (
	                SELECT distinct c.id FROM changeset c
	                INNER JOIN attribute a ON a.changeset_id = c.id
	                WHERE c.timestamp >= @delete_threshold
	                OR a.timestamp >= @delete_threshold
	                OR (a.removed = false AND a.id IN (
		                select distinct on(layer_id, ci_id, name) id FROM attribute
				                where timestamp <= @now
				                order by layer_id, ci_id, name, timestamp DESC NULLS LAST
	                ))
	                UNION
	                SELECT distinct c.id FROM changeset c
	                INNER JOIN relation r ON r.changeset_id = c.id
	                WHERE c.timestamp >= @delete_threshold
	                OR r.timestamp >= @delete_threshold
	                OR (r.removed = false AND r.id IN (
		                select distinct on(layer_id, from_ci_id, to_ci_id, predicate_id) id FROM relation
				                where timestamp <= @now
				                order by layer_id, from_ci_id, to_ci_id, predicate_id, timestamp DESC NULLS LAST
	                ))
                )";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);

            var now = TimeThreshold.BuildLatest();
            command.Parameters.AddWithValue("delete_threshold", threshold.ToUniversalTime());
            command.Parameters.AddWithValue("now", now.Time.ToUniversalTime());
            command.Prepare();

            var numArchived = await command.ExecuteNonQueryAsync();

            return numArchived;
        }

        public async Task<long> GetNumberOfChangesets(IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT count(*) FROM changeset c", trans.DBConnection, trans.DBTransaction);
            command.Prepare();
            var ret = (long?)await command.ExecuteScalarAsync();
            return ret!.Value;
        }

        public async Task<long> GetNumberOfChangesets(string layerID, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT count(*) FROM changeset c WHERE layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Prepare();
            var ret = (long?)await command.ExecuteScalarAsync();
            return ret!.Value;
        }
        public async Task<IReadOnlyList<Changeset>> GetChangesetsAfter(Guid afterChangesetID, string[] layerIDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            var query = @"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp > (SELECT i.timestamp FROM changeset i WHERE i.id = @changeset_id LIMIT 1) AND c.timestamp <= @threshold
                AND c.layer_id = ANY(@layer_ids)
                ORDER BY c.timestamp DESC NULLS LAST";
            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", afterChangesetID);
            command.Parameters.AddWithValue("layer_ids", layerIDs);
            command.Parameters.AddWithValue("threshold", timeThreshold.Time.ToUniversalTime());
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetGuid(0);
                var userID = dr.GetInt64(1);
                var layerID = dr.GetString(2);
                var dataOriginType = dr.GetFieldValue<DataOriginType>(3);
                var origin = new DataOriginV1(dataOriginType);
                var timestamp = dr.GetDateTime(4);
                var username = dr.GetString(5);
                var displayName = dr.GetString(6);
                var userUUID = dr.GetGuid(7);
                var userType = dr.GetFieldValue<UserType>(8);
                var userTimestamp = dr.GetDateTime(9);

                var user = new UserInDatabase(userID, userUUID, username, displayName, userType, userTimestamp);
                var c = new Changeset(id, user, layerID, origin, timestamp);
                ret.Add(c);
            }
            return ret;
        }
    }
}
