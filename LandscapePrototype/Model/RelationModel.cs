using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class RelationModel
    {
        private readonly NpgsqlConnection conn;

        public RelationModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public enum IncludeRelationDirections
        {
            Forward, Backward, Both
        }

        public IEnumerable<Relation> GetMergedRelations(string ciIdentity, bool includeRemoved, LayerSet layers, IncludeRelationDirections ird)
        {
            if (ird != IncludeRelationDirections.Forward)
                throw new NotImplementedException(); // TODO: implement

            var ret = new List<Relation>();

            LayerSet.CreateLayerSetTempTable(layers, "temp_layerset", conn).GetAwaiter().GetResult(); ;

            using (var command = new NpgsqlCommand(@"
            select distinct
            last_value(inn.last_from_ci_id) over wndOut,
            last_value(inn.last_to_ci_id) over wndOut,
            last_value(inn.last_predicate) over wndOut,
            last_value(inn.last_activation_time) over wndOut,
            last_value(inn.last_layer_id) over wndOut,
            last_value(inn.last_state) over wndOut,
            last_value(inn.last_changeset_id) over wndOut
            FROM (
                select distinct
                last_value(r.from_ci_id) over wnd as last_from_ci_id,
                last_value(r.to_ci_id) over wnd as last_to_ci_id,
                last_value(r.predicate) over wnd as last_predicate,
                last_value(r.activation_time) over wnd as last_activation_time,
                last_value(r.layer_id) over wnd as last_layer_id,
                last_value(r.state) over wnd as last_state,
                last_value(r.changeset_id) over wnd as last_changeset_id
                    from relation r
                    inner join ci c ON r.from_ci_id = c.id
                    where r.activation_time <= now() and c.identity = @from_ci_identity
                WINDOW wnd AS(
                    PARTITION by r.from_ci_id, r.to_ci_id, r.predicate, r.layer_id ORDER BY r.activation_time ASC  -- sort by activation time
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ) inn
            inner join temp_layerset ls ON inn.last_layer_id = ls.id -- inner join to only keep rows that are in the selected layers
            where inn.last_state != ALL(@excluded_states) -- remove entries from layers which' last item is deleted
            WINDOW wndOut AS(PARTITION by inn.last_from_ci_id, inn.last_to_ci_id, inn.last_predicate ORDER BY ls.order DESC -- sort by layer order
                ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
            ", conn))
            {
                command.Parameters.AddWithValue("from_ci_identity", ciIdentity);
                var excludedStates = (includeRemoved) ? new RelationState[] { } : new RelationState[] { RelationState.Removed };
                command.Parameters.AddWithValue("excluded_states", excludedStates);
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var fromCIID = dr.GetInt64(0);
                    var toCIID = dr.GetInt64(1);
                    var predicate = dr.GetString(2);
                    var activationTime = dr.GetTimeStamp(3).ToDateTime();
                    var layerID = dr.GetInt64(4);
                    var state = dr.GetFieldValue<RelationState>(5);
                    var changesetID = dr.GetInt64(6);

                    var relation = Relation.Build(fromCIID, toCIID, predicate, activationTime, layerID, state, changesetID);

                    ret.Add(relation);
                }
            }
            return ret;
        }


        public Relation GetRelation(long fromCIID, long toCIID, string predicate, long layerID)
        {
            using (var command = new NpgsqlCommand(@"select activation_time, state, changeset_id from relation 
                where from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id AND predicate = @predicate AND layer_id = @layer_id LIMIT 1", conn))
            {
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate", predicate);
                command.Parameters.AddWithValue("layer_id", layerID);
                using var dr = command.ExecuteReader();
                if (!dr.Read())
                    return null;

                var activationTime = dr.GetTimeStamp(0).ToDateTime();
                var state = dr.GetFieldValue<RelationState>(1);
                var changesetID = dr.GetInt64(2);

                return Relation.Build(fromCIID, toCIID, predicate, activationTime, layerID, state, changesetID);
            }
        }

        public bool RemoveRelation(long fromCIID, long toCIID, string predicate, long layerID, long changesetID)
        {
            var currentRelation = GetRelation(fromCIID, toCIID, predicate, layerID);

            if (currentRelation == null)
            {
                // relation does not exist
                return false;
            }
            if (currentRelation.State == RelationState.Removed)
            {
                // the relation is already removed, no-op(?)
                return true;
            }

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate, activation_time, layer_id, state, changeset_id) 
                VALUES (@from_ci_id, @to_ci_id, @predicate, now(), @layer_id, @state, @changeset_id)", conn);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate", predicate);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", RelationState.Removed);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var numInserted = command.ExecuteNonQuery();
            return numInserted == 1;
        }

        public bool InsertRelation(long fromCIID, long toCIID, string predicate, long layerID, long changesetID)
        {
            var currentRelation = GetRelation(fromCIID, toCIID, predicate, layerID);

            var state = RelationState.New;
            if (currentRelation != null)
            {
                if (currentRelation.State == RelationState.Removed)
                    state = RelationState.Renewed;
                else
                {
                    // same predicate already exists and is present // TODO: think about different user inserting
                    return true;
                }
            }

            using var command = new NpgsqlCommand(@"INSERT INTO relation (from_ci_id, to_ci_id, predicate, activation_time, layer_id, state, changeset_id) 
                VALUES (@from_ci_id, @to_ci_id, @predicate, now(), @layer_id, @state, @changeset_id)", conn);

            command.Parameters.AddWithValue("from_ci_id", fromCIID);
            command.Parameters.AddWithValue("to_ci_id", toCIID);
            command.Parameters.AddWithValue("predicate", predicate);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("changeset_id", changesetID);

            var numInserted = command.ExecuteNonQuery();

            return numInserted == 1;
        }
    }
}
