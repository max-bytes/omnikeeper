using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class PredicateModel : IPredicateModel
    {
        private readonly NpgsqlConnection conn;

        public PredicateModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<string> CreatePredicate(string id, string wordingFrom, string wordingTo, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO predicate (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();

            using var commandWording = new NpgsqlCommand(@"INSERT INTO predicate_wording (predicate_id, wording_from, wording_to, ""timestamp"")
                VALUES (@predicate_id, @wording_from, @wording_to, now())", conn, trans);
            commandWording.Parameters.AddWithValue("predicate_id", id);
            commandWording.Parameters.AddWithValue("wording_from", wordingFrom);
            commandWording.Parameters.AddWithValue("wording_to", wordingTo);
            await commandWording.ExecuteNonQueryAsync();

            return id;
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, DateTimeOffset? atTime)
        {
            var ret = new Dictionary<string, Predicate>();
            using var command = new NpgsqlCommand(@"SELECT 
                    last_value(p.id) over wnd, 
                    last_value(pw.wording_from) over wnd, 
                    last_value(pw.wording_to) over wnd
                FROM predicate p
                LEFT JOIN predicate_wording pw ON pw.predicate_id = p.id AND pw.timestamp <= @atTime
                WINDOW wnd AS(
                    PARTITION by p.id ORDER BY pw.timestamp
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn, trans);
            var finalTimeThreshold = atTime ?? DateTimeOffset.Now;
            command.Parameters.AddWithValue("atTime", finalTimeThreshold);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    var wordingFrom = (s.IsDBNull(1)) ? "relates to" : s.GetString(1);
                    var wordingTo = (s.IsDBNull(2)) ? "is being related to from" : s.GetString(2);
                    ret.Add(id, Predicate.Build(id, wordingFrom, wordingTo));
                }
            }
            return ret;
        }
    }
}
