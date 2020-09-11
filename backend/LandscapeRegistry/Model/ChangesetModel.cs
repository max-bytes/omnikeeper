using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Landscape.Base.Model.IChangesetModel;

namespace LandscapeRegistry.Model
{
    public class ChangesetModel : IChangesetModel
    {
        private readonly NpgsqlConnection conn;
        private readonly IUserInDatabaseModel userModel;

        public ChangesetModel(IUserInDatabaseModel userModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.userModel = userModel;
        }

        public async Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans, DateTimeOffset? timestamp = null)
        {
            var user = await userModel.GetUser(userID, trans);
            if (user == null)
                throw new Exception($"Could not find user with ID {userID}");
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (id, timestamp, user_id) VALUES (@id, @timestamp, @user_id) returning timestamp", conn, trans);
            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("user_id", userID);
            command.Parameters.AddWithValue("timestamp", timestamp.GetValueOrDefault(DateTimeOffset.Now));
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var timestampR = reader.GetDateTime(0);
            return Changeset.Build(id, user, timestampR);
        }

        public async Task<Changeset> GetChangeset(Guid id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT c.timestamp, c.user_id, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.id = @id", conn, trans);

            command.Parameters.AddWithValue("id", id);
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var timestamp = dr.GetTimeStamp(0).ToDateTime();
            var userID = dr.GetInt64(1);
            var username = dr.GetString(2);
            var displayName = dr.GetString(3);
            var keycloakUUID = dr.GetGuid(4);
            var userType = dr.GetFieldValue<UserType>(5);
            var userTimestamp = dr.GetTimeStamp(6).ToDateTime();

            var user = UserInDatabase.Build(userID, keycloakUUID, username, displayName, userType, userTimestamp);
            return Changeset.Build(id, user, timestamp);
        }

        // returns all changesets in the time range
        // sorted by timestamp
        public async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IChangesetSelection cs, NpgsqlTransaction trans, int? limit = null)
        {
            return cs switch
            {
                ChangesetSelectionSingleCI sci => await GetChangesetsInTimespan(from, to, layers, new Guid[] { sci.ciid }, trans, limit),
                ChangesetSelectionMultipleCIs mci => await GetChangesetsInTimespan(from, to, layers, mci.CIIDs, trans, limit),
                ChangesetSelectionAllCIs _ => await GetChangesetsInTimespan(from, to, layers, trans, limit),
                _ => throw new Exception("Invalid changeset selection"),
            };
        }


        // returns all changesets affecting this CI, both via attributes OR relations
        // sorted by timestamp
        private async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, Guid[] ciids, NpgsqlTransaction trans, int? limit = null)
        {
            var queryAttributes = @"SELECT distinct c.id, c.user_id, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                INNER JOIN attribute a ON a.changeset_id = c.id 
                INNER JOIN ci ci ON a.ci_id = ci.id
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND a.layer_id = ANY(@layer_ids)";
            queryAttributes += " AND ci.id = ANY(@ciids)";

            var irdClause = "r.from_ci_id = ci.id OR r.to_ci_id = ci.id";
            var queryRelations = $@"SELECT distinct c.id, c.user_id, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                INNER JOIN relation r ON r.changeset_id = c.id 
                INNER JOIN ci ci ON ({irdClause})
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND r.layer_id = ANY(@layer_ids)";
            queryRelations += " AND ci.id = ANY(@ciids)";

            var query = @$" {queryAttributes} UNION {queryRelations} ORDER BY 3 DESC";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, conn, trans);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
            command.Parameters.AddWithValue("ciids", ciids);
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            if (limit.HasValue)
                command.Parameters.AddWithValue("limit", limit.Value);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetGuid(0);
                var userID = dr.GetInt64(1);
                var timestamp = dr.GetTimeStamp(2).ToDateTime();
                var username = dr.GetString(3);
                var displayName = dr.GetString(4);
                var userUUID = dr.GetGuid(5);
                var userType = dr.GetFieldValue<UserType>(6);
                var userTimestamp = dr.GetTimeStamp(7).ToDateTime();

                var user = UserInDatabase.Build(userID, userUUID, username, displayName, userType, userTimestamp);
                var c = Changeset.Build(id, user, timestamp);
                ret.Add(c);
            }
            return ret;
        }

        private async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, NpgsqlTransaction trans, int? limit = null)
        {
            var query = @"SELECT distinct c.id, c.user_id, c.timestamp, u.username, u.displayName, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                LEFT JOIN attribute a ON a.changeset_id = c.id 
                LEFT JOIN relation r ON r.changeset_id = c.id
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to
                AND
                    (EXISTS(SELECT * FROM attribute a WHERE a.changeset_id = c.id AND a.layer_id = ANY(@layer_ids))
                    OR EXISTS(SELECT * FROM relation r WHERE r.changeset_id = c.id AND r.layer_id = ANY(@layer_ids)))
                ORDER BY c.timestamp DESC";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, conn, trans);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            if (limit.HasValue)
                command.Parameters.AddWithValue("limit", limit.Value);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetGuid(0);
                var userID = dr.GetInt64(1);
                var timestamp = dr.GetTimeStamp(2).ToDateTime();
                var username = dr.GetString(3);
                var displayName = dr.GetString(4);
                var userUUID = dr.GetGuid(5);
                var userType = dr.GetFieldValue<UserType>(6);
                var userTimestamp = dr.GetTimeStamp(7).ToDateTime();

                var user = UserInDatabase.Build(userID, userUUID, username, displayName, userType, userTimestamp);
                var c = Changeset.Build(id, user, timestamp);
                ret.Add(c);
            }
            return ret;
        }
    }
}
