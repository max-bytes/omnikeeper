using Landscape.Base.Model;
using LandscapePrototype.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LandscapePrototype.Model.RelationModel;

namespace LandscapePrototype.Model
{
    public class ChangesetModel : IChangesetModel
    {
        private readonly NpgsqlConnection conn;

        public ChangesetModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<Changeset> CreateChangeset(string username, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (timestamp, username) VALUES (now(), @username) returning id, timestamp", conn, trans);
            command.Parameters.AddWithValue("username", username);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            var timestamp = reader.GetDateTime(1);
            return Changeset.Build(id, username, timestamp);
        }

        public async Task<Changeset> GetChangeset(long id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT timestamp, username FROM changeset WHERE id = @id", conn, trans);

            command.Parameters.AddWithValue("id", id);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            if (!await dr.ReadAsync())
                return null;

            var timestamp = dr.GetTimeStamp(0).ToDateTime();
            var username = dr.GetString(1);
            return Changeset.Build(id, username, timestamp);
        }

        // if ciid != null, returns all changesets affecting this CI, both via attributes OR relations
        // sorted by timestamp
        public async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IncludeRelationDirections ird, string ciid, NpgsqlTransaction trans)
        {
            var queryAttributes = @"SELECT distinct c.id, c.username, c.timestamp FROM changeset c 
                INNER JOIN attribute a ON a.changeset_id = c.id 
                INNER JOIN ci ci ON a.ci_id = ci.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND a.layer_id = ANY(@layer_ids)";
            if (ciid != null) queryAttributes += " AND ci.id = @ciid"; // TODO: performance improvements when ciid === null, ci join unnecessary then

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
            var queryRelations = $@"SELECT distinct c.id, c.username, c.timestamp FROM changeset c 
                INNER JOIN relation r ON r.changeset_id = c.id 
                INNER JOIN ci ci ON ({irdClause})
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND r.layer_id = ANY(@layer_ids)";
            if (ciid != null) queryRelations += " AND ci.id = @ciid"; // TODO: performance improvements when ciid === null, ci join unnecessary then

            var query = @$" {queryAttributes} UNION {queryRelations}";

            using var command = new NpgsqlCommand(query, conn, trans);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
            if (ciid != null)
                command.Parameters.AddWithValue("ciid", ciid);
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetInt64(0);
                var username = dr.GetString(1);
                var timestamp = dr.GetTimeStamp(2).ToDateTime();
                var c = Changeset.Build(id, username, timestamp);
                ret.Add(c);
            }
            return ret.OrderBy(x => x.Timestamp);
        }
    }
}
