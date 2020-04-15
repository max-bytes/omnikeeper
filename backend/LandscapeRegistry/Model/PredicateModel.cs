using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Landscape.Base.Model.IPredicateModel;

namespace LandscapeRegistry.Model
{
    public class PredicateModel : IPredicateModel
    {
        private readonly NpgsqlConnection conn;

        private static readonly string DefaultWordingFrom = "relates to";
        private static readonly string DefaultWordingTo = "is being related to from";
        private static readonly PredicateState DefaultState = PredicateState.Active;

        public PredicateModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        private async Task<Predicate> Insert(string id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO predicate (id) VALUES (@id)", conn, trans);
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();

            return Predicate.Build(id, DefaultWordingFrom, DefaultWordingTo, DefaultState);
        }

        public async Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, PredicateState state, NpgsqlTransaction trans)
        {
            var current = await GetPredicate(id, trans);

            if (current == null)
                current = await Insert(id, trans);

            // update wordings
            if (current.WordingFrom != wordingFrom || current.WordingTo != wordingTo)
            {
                using var commandWording = new NpgsqlCommand(@"INSERT INTO predicate_wording (predicate_id, wording_from, wording_to, ""timestamp"")
                    VALUES (@predicate_id, @wording_from, @wording_to, now())", conn, trans);
                commandWording.Parameters.AddWithValue("predicate_id", id);
                commandWording.Parameters.AddWithValue("wording_from", wordingFrom);
                commandWording.Parameters.AddWithValue("wording_to", wordingTo);
                await commandWording.ExecuteNonQueryAsync();
                current = Predicate.Build(id, wordingFrom, wordingTo, current.State);
            }

            // update state
            if (current.State != state)
            {
                using var commandState = new NpgsqlCommand(@"INSERT INTO predicate_state (predicate_id, state, ""timestamp"")
                    VALUES (@predicate_id, @state, now())", conn, trans);
                commandState.Parameters.AddWithValue("predicate_id", id);
                commandState.Parameters.AddWithValue("state", state);
                await commandState.ExecuteNonQueryAsync();
                current = Predicate.Build(id, current.WordingFrom, current.WordingTo, state);
            }

            return current;
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
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, DateTimeOffset? atTime, PredicateStateFilter stateFilter)
        {
            var ret = new Dictionary<string, Predicate>();
            using var command = new NpgsqlCommand(@"
                SELECT p.id, pw.wording_from, pw.wording_to, ps.state
                FROM predicate p
                LEFT JOIN 
                    (SELECT DISTINCT ON (predicate_id) predicate_id, wording_from, wording_to FROM predicate_wording ORDER BY predicate_id, timestamp DESC) pw
                    ON pw.predicate_id = p.id
               LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, state from predicate_state ORDER BY predicate_id, timestamp DESC) ps
                    ON ps.predicate_id = p.id
                WHERE (ps.state = ANY(@states) OR ps.state IS NULL)
            ", conn, trans);
            var finalTimeThreshold = atTime ?? DateTimeOffset.Now;
            command.Parameters.AddWithValue("atTime", finalTimeThreshold);
            var states = stateFilter switch
            {
                PredicateStateFilter.ActiveOnly => new PredicateState[] { PredicateState.Active },
                PredicateStateFilter.ActiveAndDeprecated => new PredicateState[] { PredicateState.Active, PredicateState.Deprecated },
                PredicateStateFilter.All => Enum.GetValues(typeof(PredicateState)),
                PredicateStateFilter.MarkedForDeletion => new PredicateState[] { PredicateState.MarkedForDeletion },
                _ => null
            };
            command.Parameters.AddWithValue("states", states);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    var wordingFrom = (s.IsDBNull(1)) ? "relates to" : s.GetString(1);
                    var wordingTo = (s.IsDBNull(2)) ? "is being related to from" : s.GetString(2);
                    var state = (s.IsDBNull(3)) ? PredicateState.Active : s.GetFieldValue<PredicateState>(3);
                    ret.Add(id, Predicate.Build(id, wordingFrom, wordingTo, state));
                }
            }
            return ret;
        }

        private async Task<Predicate> GetPredicate(string id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT pw.wording_from, pw.wording_to, ps.state
                FROM predicate p
                LEFT JOIN 
                    (SELECT DISTINCT ON (predicate_id) predicate_id, wording_from, wording_to FROM predicate_wording ORDER BY predicate_id, timestamp DESC) pw
                    ON pw.predicate_id = p.id
               LEFT JOIN
                    (SELECT DISTINCT ON (predicate_id) predicate_id, state from predicate_state ORDER BY predicate_id, timestamp DESC) ps
                    ON ps.predicate_id = p.id
                WHERE p.id = @id
            ", conn, trans);
            command.Parameters.AddWithValue("id", id);
            using var s = await command.ExecuteReaderAsync();
            if (!await s.ReadAsync())
                return null;
            var wordingFrom = (s.IsDBNull(0)) ? DefaultWordingFrom : s.GetString(0);
            var wordingTo = (s.IsDBNull(1)) ? DefaultWordingTo : s.GetString(1);
            var state = (s.IsDBNull(2)) ? DefaultState : s.GetFieldValue<PredicateState>(2);
            return Predicate.Build(id, wordingFrom, wordingTo, state);
        }
    }
}
