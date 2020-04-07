using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.Model
{
    public class ChangesetModel : IChangesetModel
    {
        private readonly NpgsqlConnection conn;
        private readonly UserInDatabaseModel userModel;

        public ChangesetModel(UserInDatabaseModel userModel, NpgsqlConnection connection)
        {
            conn = connection;
            this.userModel = userModel;
        }

        public async Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans)
        {
            var user = await userModel.GetUser(userID, trans);
            if (user == null)
                return null;
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (timestamp, user_id) VALUES (now(), @user_id) returning id, timestamp", conn, trans);
            command.Parameters.AddWithValue("user_id", userID);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            var timestamp = reader.GetDateTime(1);
            return Changeset.Build(id, user, timestamp);
        }

        public async Task<Changeset> GetChangeset(long id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT c.timestamp, c.user_id, u.username, u.keycloak_id, u.type, u.timestamp FROM changeset c
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.id = @id", conn, trans);

            command.Parameters.AddWithValue("id", id);
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var timestamp = dr.GetTimeStamp(0).ToDateTime();
            var userID = dr.GetInt64(1);
            var username = dr.GetString(2);
            var userUUID = dr.GetGuid(3);
            var userType = dr.GetFieldValue<UserType>(4);
            var userTimestamp = dr.GetTimeStamp(5).ToDateTime();

            var user = UserInDatabase.Build(userID, userUUID, username, userType, userTimestamp);
            return Changeset.Build(id, user, timestamp);
        }

        // returns all changesets affecting this CI, both via attributes OR relations
        // sorted by timestamp
        public async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IncludeRelationDirections ird, string ciid, NpgsqlTransaction trans, int? limit = null)
        {
            var queryAttributes = @"SELECT distinct c.id, c.user_id, c.timestamp, u.username, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                INNER JOIN attribute a ON a.changeset_id = c.id 
                INNER JOIN ci ci ON a.ci_id = ci.id
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND a.layer_id = ANY(@layer_ids)";
            queryAttributes += " AND ci.id = @ciid";

            string irdClause;
            switch (ird)
            {
                case IncludeRelationDirections.Forward:
                    irdClause = "r.from_ci_id = ci.id";
                    break;
                case IncludeRelationDirections.Backward:
                    irdClause = "r.to_ci_id = ci.id";
                    break;
                case IncludeRelationDirections.Both:
                    irdClause = "r.from_ci_id = ci.id OR r.to_ci_id = ci.id";
                    break;
                default:
                    irdClause = "unused";
                    break;
            }
            var queryRelations = $@"SELECT distinct c.id, c.user_id, c.timestamp, u.username, u.keycloak_id, u.type, u.timestamp FROM changeset c 
                INNER JOIN relation r ON r.changeset_id = c.id 
                INNER JOIN ci ci ON ({irdClause})
                LEFT JOIN ""user"" u ON c.user_id = u.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND r.layer_id = ANY(@layer_ids)";
            queryRelations += " AND ci.id = @ciid";

            var query = @$" {queryAttributes} UNION {queryRelations} ORDER BY 3 DESC";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, conn, trans);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
            command.Parameters.AddWithValue("ciid", ciid);
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            if (limit.HasValue)
                command.Parameters.AddWithValue("limit", limit.Value);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetInt64(0);
                var userID = dr.GetInt64(1);
                var timestamp = dr.GetTimeStamp(2).ToDateTime();

                var username = dr.GetString(3);
                var userUUID = dr.GetGuid(4);
                var userType = dr.GetFieldValue<UserType>(5);
                var userTimestamp = dr.GetTimeStamp(6).ToDateTime();

                var user = UserInDatabase.Build(userID, userUUID, username, userType, userTimestamp);
                var c = Changeset.Build(id, user, timestamp);
                ret.Add(c);
            }
            return ret;
        }


        public async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IncludeRelationDirections ird, NpgsqlTransaction trans, int? limit = null)
        {
            var query = @"SELECT distinct c.id, c.user_id, c.timestamp, u.username, u.keycloak_id, u.type, u.timestamp FROM changeset c 
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
                var id = dr.GetInt64(0);
                var userID = dr.GetInt64(1);
                var timestamp = dr.GetTimeStamp(2).ToDateTime();

                var username = dr.GetString(3);
                var userUUID = dr.GetGuid(4);
                var userType = dr.GetFieldValue<UserType>(5);
                var userTimestamp = dr.GetTimeStamp(6).ToDateTime();

                var user = UserInDatabase.Build(userID, userUUID, username, userType, userTimestamp);
                var c = Changeset.Build(id, user, timestamp);
                ret.Add(c);
            }
            return ret;
        }
    }
}
