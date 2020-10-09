using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class PredicateModel : IPredicateModel
    {
        private readonly NpgsqlConnection conn;

        private static readonly string DefaultWordingFrom = "relates to";
        private static readonly string DefaultWordingTo = "is being related to from";
        private static readonly AnchorState DefaultState = AnchorState.Active;
        public static readonly PredicateConstraints DefaultConstraits = PredicateConstraints.Default;

        public PredicateModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        private async Task<Predicate> Insert(string id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO predicate (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();

            return Predicate.Build(id, DefaultWordingFrom, DefaultWordingTo, DefaultState, DefaultConstraits);
        }

        public async Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, NpgsqlTransaction trans, DateTimeOffset? timestamp = null)
        {
            var current = await GetPredicate(id, trans);
            var changed = false;

            if (current == null)
            {
                current = await Insert(id, trans);
                changed = true;
            }

            if (timestamp == null)
                timestamp = DateTimeOffset.Now;

            // update wordings
            if (current.WordingFrom != wordingFrom || current.WordingTo != wordingTo)
            {
                using var commandWording = new NpgsqlCommand(@"INSERT INTO predicate_wording (predicate_id, wording_from, wording_to, ""timestamp"")
                    VALUES (@predicate_id, @wording_from, @wording_to, @timestamp)", conn, trans);
                commandWording.Parameters.AddWithValue("predicate_id", id);
                commandWording.Parameters.AddWithValue("wording_from", wordingFrom);
                commandWording.Parameters.AddWithValue("wording_to", wordingTo);
                commandWording.Parameters.AddWithValue("timestamp", timestamp);
                await commandWording.ExecuteNonQueryAsync();
                current = Predicate.Build(id, wordingFrom, wordingTo, current.State, current.Constraints);

                changed = true;
            }

            // update state
            if (current.State != state)
            {
                using var commandState = new NpgsqlCommand(@"INSERT INTO predicate_state (predicate_id, state, ""timestamp"")
                    VALUES (@predicate_id, @state, @timestamp)", conn, trans);
                commandState.Parameters.AddWithValue("predicate_id", id);
                commandState.Parameters.AddWithValue("state", state);
                commandState.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandState.ExecuteNonQueryAsync();

                current = Predicate.Build(id, current.WordingFrom, current.WordingTo, state, current.Constraints);
                changed = true;
            }

            // update constraits
            if (!current.Constraints.Equals(constraints))
            {
                using var commandConstraints = new NpgsqlCommand(@"INSERT INTO predicate_constraints (predicate_id, constraints, ""timestamp"")
                    VALUES (@predicate_id, @constraints, @timestamp)", conn, trans);
                commandConstraints.Parameters.AddWithValue("predicate_id", id);
                commandConstraints.Parameters.AddWithValue("constraints", NpgsqlDbType.Json, constraints);
                //commandConstraints.Parameters.Add(new NpgsqlParameter("constraints", NpgsqlDbType.Json) { Value = constraints });
                commandConstraints.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
                await commandConstraints.ExecuteNonQueryAsync();
                current = Predicate.Build(id, current.WordingFrom, current.WordingTo, state, constraints);
                changed = true;
            }

            return (current, changed);
        }

        public async Task<bool> TryToDelete(string id, NpgsqlTransaction trans)
        {
            try
            {
                using var command = new NpgsqlCommand(@"DELETE FROM predicate WHERE id = @id", conn, trans);
                command.Parameters.AddWithValue("id", id);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (PostgresException e)
            {
                return false;
            }
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, TimeThreshold atTime, AnchorStateFilter stateFilter)
        {
            var ret = new Dictionary<string, Predicate>();

            using var command = new NpgsqlCommand(@"
                SELECT p.id, pw.wording_from, pw.wording_to, ps.state, pc.constraints
                FROM predicate p
                LEFT JOIN 
                    (SELECT DISTINCT ON (predicate_id) predicate_id, wording_from, wording_to, timestamp FROM predicate_wording WHERE timestamp <= @at_time ORDER BY predicate_id, timestamp DESC) pw
                    ON pw.predicate_id = p.id
                LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, state, timestamp FROM predicate_state WHERE timestamp <= @at_time ORDER BY predicate_id, timestamp DESC) ps
                    ON ps.predicate_id = p.id
                LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, constraints, timestamp FROM predicate_constraints WHERE timestamp <= @at_time ORDER BY predicate_id, timestamp DESC) pc
                    ON pc.predicate_id = p.id
                WHERE (ps.state = ANY(@states) OR (ps.state IS NULL AND @default_state = ANY(@states)))
            ", conn, trans);

            command.Parameters.AddWithValue("at_time", atTime.Time);
            command.Parameters.AddWithValue("states", stateFilter.Filter2States());
            command.Parameters.AddWithValue("default_state", DefaultState);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    var wordingFrom = (s.IsDBNull(1)) ? DefaultWordingFrom : s.GetString(1);
                    var wordingTo = (s.IsDBNull(2)) ? DefaultWordingTo : s.GetString(2);
                    var state = (s.IsDBNull(3)) ? DefaultState : s.GetFieldValue<AnchorState>(3);
                    var constraints = PredicateConstraints.Default;
                    try
                    {
                        if (!s.IsDBNull(4))
                            constraints = s.GetFieldValue<PredicateConstraints>(4);
                    }
                    catch (System.Text.Json.JsonException e)
                    {
                        // TODO: error handling?
                    }

                    ret.Add(id, Predicate.Build(id, wordingFrom, wordingTo, state, constraints));
                }
            }
            return ret;
        }

        public async Task<Predicate> GetPredicate(string id, TimeThreshold atTime, AnchorStateFilter stateFilter, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT pw.wording_from, pw.wording_to, ps.state, pc.constraints
                FROM predicate p
                LEFT JOIN 
                    (SELECT DISTINCT ON (predicate_id) predicate_id, wording_from, wording_to FROM predicate_wording WHERE timestamp <= @atTime ORDER BY predicate_id, timestamp DESC) pw
                    ON pw.predicate_id = p.id
                LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, state from predicate_state WHERE timestamp <= @atTime ORDER BY predicate_id, timestamp DESC) ps
                    ON ps.predicate_id = p.id
                LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, constraints from predicate_constraints WHERE timestamp <= @atTime ORDER BY predicate_id, timestamp DESC) pc
                    ON pc.predicate_id = p.id
                WHERE p.id = @id AND ((ps.state = ANY(@states) OR (ps.state IS NULL AND @default_state = ANY(@states))))
            ", conn, trans);

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("atTime", atTime.Time);
            command.Parameters.AddWithValue("states", stateFilter.Filter2States());
            command.Parameters.AddWithValue("default_state", DefaultState);

            using var s = await command.ExecuteReaderAsync();
            if (!await s.ReadAsync())
                return null;

            var wordingFrom = (s.IsDBNull(0)) ? DefaultWordingFrom : s.GetString(0);
            var wordingTo = (s.IsDBNull(1)) ? DefaultWordingTo : s.GetString(1);
            var state = (s.IsDBNull(2)) ? DefaultState : s.GetFieldValue<AnchorState>(2);
            var constraints = (s.IsDBNull(3)) ? PredicateConstraints.Default : s.GetFieldValue<PredicateConstraints>(3); // TODO: what if the json cannot be parsed?
            return Predicate.Build(id, wordingFrom, wordingTo, state, constraints);
        }

        private async Task<Predicate> GetPredicate(string id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT pw.wording_from, pw.wording_to, ps.state, pc.constraints
                FROM predicate p
                LEFT JOIN 
                    (SELECT DISTINCT ON (predicate_id) predicate_id, wording_from, wording_to FROM predicate_wording ORDER BY predicate_id, timestamp DESC) pw
                    ON pw.predicate_id = p.id
                LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, state from predicate_state ORDER BY predicate_id, timestamp DESC) ps
                    ON ps.predicate_id = p.id
                LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, constraints from predicate_constraints ORDER BY predicate_id, timestamp DESC) pc
                    ON pc.predicate_id = p.id
                WHERE p.id = @id
            ", conn, trans);
            command.Parameters.AddWithValue("id", id);
            using var s = await command.ExecuteReaderAsync();
            if (!await s.ReadAsync())
                return null;
            var wordingFrom = (s.IsDBNull(0)) ? DefaultWordingFrom : s.GetString(0);
            var wordingTo = (s.IsDBNull(1)) ? DefaultWordingTo : s.GetString(1);
            var state = (s.IsDBNull(2)) ? DefaultState : s.GetFieldValue<AnchorState>(2);
            var contraints = (s.IsDBNull(3)) ? PredicateConstraints.Default : s.GetFieldValue<PredicateConstraints>(3); // TODO: what if the json cannot be parsed?
            return Predicate.Build(id, wordingFrom, wordingTo, state, contraints);
        }
    }
}
