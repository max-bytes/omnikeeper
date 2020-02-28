using LandscapePrototype.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class ChangesetModel
    {
        private readonly NpgsqlConnection conn;

        public ChangesetModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<long> CreateChangeset(NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (timestamp) VALUES (now()) returning id", conn, trans);
            var id = (long)await command.ExecuteScalarAsync();
            return id;
        }

        public async Task<Changeset> GetChangeset(long id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT timestamp FROM changeset WHERE id = @id", conn, trans);

            command.Parameters.AddWithValue("id", id);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            if (!await dr.ReadAsync())
                return null;

            var timestamp = dr.GetTimeStamp(0).ToDateTime();
            return Changeset.Build(id, timestamp);
        }

        //public async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, NpgsqlTransaction trans)
        //{
        //    using var command = new NpgsqlCommand(@"SELECT id, timestamp FROM changeset WHERE timestamp >= @from AND timestamp <= @to", conn, trans);

        //    command.Parameters.AddWithValue("from", from);
        //    command.Parameters.AddWithValue("to", to);
        //    using var dr = await command.ExecuteReaderAsync();

        //    var ret = new List<Changeset>();
        //    while (await dr.ReadAsync())
        //    {
        //        var id = dr.GetInt64(0);
        //        var timestamp = dr.GetTimeStamp(1).ToDateTime();
        //        var c = Changeset.Build(id, timestamp);
        //        ret.Add(c);
        //    }
        //    return ret;
        //}

        public async Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, long? ciid, NpgsqlTransaction trans)
        {
            var query = @"SELECT c.id, c.timestamp FROM changeset c 
                INNER JOIN attribute a ON a.changeset_id = c.id 
                INNER JOIN ci ci ON a.ci_id = ci.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND a.layer_id = ANY(@layer_ids)";
            if (ciid.HasValue) query += " AND ci.id = @ciid";
            using var command = new NpgsqlCommand(query, conn, trans);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
            if (ciid.HasValue)
                command.Parameters.AddWithValue("ciid", ciid.Value);
            command.Parameters.AddWithValue("layer_ids", layers.LayerIDs);
            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<Changeset>();
            while (await dr.ReadAsync())
            {
                var id = dr.GetInt64(0);
                var timestamp = dr.GetTimeStamp(1).ToDateTime();
                var c = Changeset.Build(id, timestamp);
                ret.Add(c);
            }
            return ret;
        }
    }
}
