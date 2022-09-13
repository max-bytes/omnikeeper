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
        public async Task<Changeset> CreateChangeset(long userID, string layerID, DataOriginV1 dataOrigin, IModelContext trans, TimeThreshold timeThreshold)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO changeset (id, timestamp, user_id, layer_id, origin_type) VALUES (@id, @timestamp, @user_id, @layer_id, @origin_type) returning timestamp", trans.DBConnection, trans.DBTransaction);
            var id = Guid.NewGuid();
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("user_id", userID);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Parameters.AddWithValue("origin_type", dataOrigin.Type);
            command.Parameters.AddWithValue("timestamp", timeThreshold.Time.ToUniversalTime());
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var timestampR = reader.GetDateTime(0);
            return new Changeset(id, userID, layerID, dataOrigin, timestampR);
        }

        public async Task<Changeset?> GetChangeset(Guid id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT c.timestamp, c.user_id, c.layer_id, c.origin_type FROM changeset c
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

            return new Changeset(id, userID, layerID, origin, timestamp);
        }

        public async Task<IReadOnlyList<Changeset>> GetChangesets(ISet<Guid> ids, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"SELECT c.id, c.timestamp, c.user_id, c.layer_id, c.origin_type FROM changeset c WHERE c.id = ANY(@ids)",
                trans.DBConnection, trans.DBTransaction);

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
                ret.Add(new Changeset(id, userID, layerID, origin, timestamp));
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
        public async Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, string[] layers, IChangesetSelection cs, IModelContext trans, int? limit = null)
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
        private async Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, string[] layers, Guid[] ciids, IModelContext trans, int? limit = null)
        {
            var queryAttributes = @"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp FROM changeset c 
                INNER JOIN attribute a ON a.changeset_id = c.id 
                INNER JOIN ci ci ON a.ci_id = ci.id
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND a.layer_id = ANY(@layer_ids)"; // TODO: why do we need to join CI table?
            queryAttributes += " AND ci.id = ANY(@ciids)";

            var irdClause = "r.from_ci_id = ci.id OR r.to_ci_id = ci.id";
            var queryRelations = $@"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp FROM changeset c 
                INNER JOIN relation r ON r.changeset_id = c.id 
                INNER JOIN ci ci ON ({irdClause})
                WHERE c.timestamp >= @from AND c.timestamp <= @to AND r.layer_id = ANY(@layer_ids)"; // TODO: why do we need to join CI table?
            queryRelations += " AND ci.id = ANY(@ciids)";

            var query = @$" {queryAttributes} UNION {queryRelations} ORDER BY 5 DESC";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("from", from.ToUniversalTime());
            command.Parameters.AddWithValue("to", to.ToUniversalTime());
            command.Parameters.AddWithValue("ciids", ciids);
            command.Parameters.AddWithValue("layer_ids", layers);
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
                var c = new Changeset(id, userID, layerID, origin, timestamp);
                ret.Add(c);
            }
            return ret;
        }

        private async Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, string[] layers, IModelContext trans, int? limit = null)
        {
            var query = @"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp FROM changeset c 
                WHERE c.timestamp >= @from AND c.timestamp <= @to
                AND c.layer_id = ANY(@layer_ids)
                ORDER BY c.timestamp DESC NULLS LAST";
            if (limit.HasValue)
                query += " LIMIT @limit";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("from", from.ToUniversalTime());
            command.Parameters.AddWithValue("to", to.ToUniversalTime());
            command.Parameters.AddWithValue("layer_ids", layers);
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
                var c = new Changeset(id, userID, layerID, origin, timestamp);
                ret.Add(c);
            }
            return ret;
        }

        private static string CIIDSelection2WhereClauseAttributes(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "a.ci_id = ANY(@ciids)",
                AllCIIDsExceptSelection _ => "a.ci_id != ANY(@ciids)",
                NoCIIDsSelection _ => "1=0",
                _ => throw new NotImplementedException("")
            };
        }
        private static string CIIDSelection2WhereClauseRelations(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "(r.from_ci_id = ANY(@ciids) OR r.to_ci_id = ANY(@ciids))",
                AllCIIDsExceptSelection _ => "(r.from_ci_id != ANY(@ciids) AND r.to_ci_id != ANY(@ciids))",
                NoCIIDsSelection _ => "1=0",
                _ => throw new NotImplementedException("")
            };
        }
        private static string CIIDSelection2WhereClauseRelationsTo(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "(r.to_ci_id = ANY(@ciids))",
                AllCIIDsExceptSelection _ => "(r.to_ci_id != ANY(@ciids))",
                NoCIIDsSelection _ => "1=0",
                _ => throw new NotImplementedException("")
            };
        }
        private static string CIIDSelection2WhereClauseRelationsFrom(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => "1=1",
                SpecificCIIDsSelection _ => "(r.from_ci_id = ANY(@ciids))",
                AllCIIDsExceptSelection _ => "(r.from_ci_id != ANY(@ciids))",
                NoCIIDsSelection _ => "1=0",
                _ => throw new NotImplementedException("")
            };
        }
        private static IEnumerable<NpgsqlParameter> CIIDSelection2Parameters(ICIIDSelection selection)
        {
            switch (selection)
            {
                case AllCIIDsSelection:
                    break;
                case SpecificCIIDsSelection s:
                    yield return new NpgsqlParameter("ciids", s.CIIDs.ToArray());
                    break;
                case AllCIIDsExceptSelection e:
                    yield return new NpgsqlParameter("ciids", e.ExceptCIIDs.ToArray());
                    break;
                case NoCIIDsSelection:
                    break;
                default:
                    throw new NotImplementedException("");
            };
        }

        private static string AttributeSelection2WhereClause(IAttributeSelection selection)
        {
            return selection switch
            {
                AllAttributeSelection _ => "1=1",
                NoAttributesSelection _ => "1=0",
                NamedAttributesSelection _ => $"a.name = ANY(@attribute_names)",
                _ => throw new NotImplementedException("")
            };
        }
        private static IEnumerable<NpgsqlParameter> AttributeSelection2Parameters(IAttributeSelection selection)
        {
            switch (selection)
            {
                case AllAttributeSelection _:
                    break;
                case NoAttributesSelection _:
                    break;
                case NamedAttributesSelection n:
                    yield return new NpgsqlParameter("attribute_names", n.AttributeNames.ToArray());
                    break;
                default:
                    throw new NotImplementedException("");
            };
        }
        private static string PredicateSelection2WhereClause(IPredicateSelection selection)
        {
            return selection switch
            {
                PredicateSelectionAll _ => "1=1",
                PredicateSelectionNone _ => "1=0",
                PredicateSelectionSpecific _ => $"r.predicate_id = ANY(@predicate_ids)",
                _ => throw new NotImplementedException("")
            };
        }
        private static IEnumerable<NpgsqlParameter> PredicateSelection2Parameters(IPredicateSelection selection)
        {
            switch (selection)
            {
                case PredicateSelectionAll _:
                    break;
                case PredicateSelectionNone _:
                    break;
                case PredicateSelectionSpecific n:
                    yield return new NpgsqlParameter("predicate_ids", n.PredicateIDs.ToArray());
                    break;
                default:
                    throw new NotImplementedException("");
            };
        }

        // TODO: test
        public async Task<IDictionary<Guid, Changeset>> GetLatestChangesetPerCI(ICIIDSelection ciSelection, IAttributeSelection attributeSelection, IPredicateSelection predicateSelection, string[] layers, IModelContext trans, TimeThreshold timeThreshold)
        {
            var latestChangesets = new Dictionary<Guid, Changeset>();

            using var command = new NpgsqlCommand($@"
                SELECT DISTINCT
                 i.ci_id as ci_id,
                 first_value(i.id) OVER win AS changeset_id,
                 first_value(i.timestamp) OVER win AS timestamp,
                 first_value(i.user_id) OVER win AS user_id,
                 first_value(i.origin_type) OVER win AS origin_type,
                 first_value(i.layer_id) OVER win AS layer_id
                FROM
                 (select c.timestamp, c.id, a.ci_id, c.user_id, c.origin_type, c.layer_id from changeset c
	                INNER JOIN attribute a ON a.changeset_id = c.id AND {AttributeSelection2WhereClause(attributeSelection)} AND {CIIDSelection2WhereClauseAttributes(ciSelection)}
	                WHERE c.timestamp <= @threshold AND c.layer_id = ANY(@layer_ids)
                 union select c.timestamp, c.id, r.from_ci_id as ci_id, c.user_id, c.origin_type, c.layer_id from changeset c
	                INNER JOIN relation r ON r.changeset_id = c.id AND {PredicateSelection2WhereClause(predicateSelection)} AND {CIIDSelection2WhereClauseRelationsFrom(ciSelection)}
	                WHERE c.timestamp <= @threshold AND c.layer_id = ANY(@layer_ids)
                 union select c.timestamp, c.id, r.to_ci_id as ci_id, c.user_id, c.origin_type, c.layer_id from changeset c
	                INNER JOIN relation r ON r.changeset_id = c.id AND {PredicateSelection2WhereClause(predicateSelection)} AND {CIIDSelection2WhereClauseRelationsTo(ciSelection)}
	                WHERE c.timestamp <= @threshold AND c.layer_id = ANY(@layer_ids)
                 ) i
                WINDOW win AS (PARTITION BY i.ci_id ORDER BY i.timestamp DESC)", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("threshold", timeThreshold.Time.ToUniversalTime());
            command.Parameters.AddWithValue("layer_ids", layers);
            foreach (var p in CIIDSelection2Parameters(ciSelection))
                command.Parameters.Add(p);
            foreach (var p in AttributeSelection2Parameters(attributeSelection))
                command.Parameters.Add(p);
            foreach (var p in PredicateSelection2Parameters(predicateSelection))
                command.Parameters.Add(p);
            using (var dr = await command.ExecuteReaderAsync())
            {
                while (await dr.ReadAsync())
                {
                    var ciid = dr.GetGuid(0);
                    var changesetID = dr.GetGuid(1);
                    var timestamp = dr.GetDateTime(2);
                    var userID = dr.GetInt64(3);
                    var dataOriginType = dr.GetFieldValue<DataOriginType>(4);
                    var layerID = dr.GetString(5);
                    var origin = new DataOriginV1(dataOriginType);
                    latestChangesets[ciid] = new Changeset(changesetID, userID, layerID, origin, timestamp);
                }
            }
            command.Dispose();

            return latestChangesets;
        }

        // NOTE: this does NOT take into account that changes happening in lower layers may not have any effect on the merged CIs
        // that means that the returned changeset may not have any practical effect in the chosen layers, because the changes it did are actually hidden by layers above
        // and hence it may not be the one that caused the last effective change
        public async Task<Changeset?> GetLatestChangesetOverall(ICIIDSelection ciSelection, IAttributeSelection attributeSelection, IPredicateSelection predicateSelection, string[] layers, IModelContext trans, TimeThreshold timeThreshold)
        {
            Changeset? latestAttributeChangeset = null;
            Changeset? latestRelationChangeset = null;

            // TODO: possible to merge into one query?

            using var commandAttributes = new NpgsqlCommand($@"SELECT c.id, c.timestamp, c.user_id, c.origin_type, c.layer_id FROM changeset c
                INNER JOIN attribute a ON a.changeset_id = c.id AND {AttributeSelection2WhereClause(attributeSelection)} AND {CIIDSelection2WhereClauseAttributes(ciSelection)}
                WHERE c.timestamp <= @threshold AND c.layer_id = ANY(@layer_ids)
                ORDER BY c.timestamp DESC
                LIMIT 1", trans.DBConnection, trans.DBTransaction);
            commandAttributes.Parameters.AddWithValue("threshold", timeThreshold.Time.ToUniversalTime());
            commandAttributes.Parameters.AddWithValue("layer_ids", layers);
            foreach (var p in CIIDSelection2Parameters(ciSelection))
                commandAttributes.Parameters.Add(p);
            foreach (var p in AttributeSelection2Parameters(attributeSelection))
                commandAttributes.Parameters.Add(p);
            using (var drAttributes = await commandAttributes.ExecuteReaderAsync())
            {
                if (await drAttributes.ReadAsync())
                {
                    var id = drAttributes.GetGuid(0);
                    var timestamp = drAttributes.GetDateTime(1);
                    var userID = drAttributes.GetInt64(2);
                    var dataOriginType = drAttributes.GetFieldValue<DataOriginType>(3);
                    var layerID = drAttributes.GetString(4);
                    var origin = new DataOriginV1(dataOriginType);
                    latestAttributeChangeset = new Changeset(id, userID, layerID, origin, timestamp);
                }
            }
            commandAttributes.Dispose();

            using var commandRelations = new NpgsqlCommand($@"SELECT c.id, c.timestamp, c.user_id, c.origin_type, c.layer_id FROM changeset c
                INNER JOIN relation r ON r.changeset_id = c.id AND {PredicateSelection2WhereClause(predicateSelection)} AND {CIIDSelection2WhereClauseRelations(ciSelection)}
                WHERE c.timestamp <= @threshold AND c.layer_id = ANY(@layer_ids)
                ORDER BY c.timestamp DESC
                LIMIT 1", trans.DBConnection, trans.DBTransaction);
            commandRelations.Parameters.AddWithValue("threshold", timeThreshold.Time.ToUniversalTime());
            commandRelations.Parameters.AddWithValue("layer_ids", layers);
            foreach (var p in CIIDSelection2Parameters(ciSelection))
                commandRelations.Parameters.Add(p);
            foreach (var p in PredicateSelection2Parameters(predicateSelection))
                commandRelations.Parameters.Add(p);
            using var drRelations = await commandRelations.ExecuteReaderAsync();
            if (await drRelations.ReadAsync())
            {
                var id = drRelations.GetGuid(0);
                var timestamp = drRelations.GetDateTime(1);
                var userID = drRelations.GetInt64(2);
                var dataOriginType = drRelations.GetFieldValue<DataOriginType>(3);
                var layerID = drRelations.GetString(4);
                var origin = new DataOriginV1(dataOriginType);
                latestRelationChangeset = new Changeset(id, userID, layerID, origin, timestamp);
            }

            if (latestAttributeChangeset == null && latestRelationChangeset == null)
                return null;
            else if (latestAttributeChangeset != null && latestRelationChangeset == null)
                return latestAttributeChangeset;
            else if (latestAttributeChangeset == null && latestRelationChangeset != null)
                return latestRelationChangeset;
            else
            {
                if (latestAttributeChangeset!.Timestamp > latestRelationChangeset!.Timestamp)
                    return latestAttributeChangeset;
                else
                    return latestRelationChangeset;
            }
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
            var query = @"SELECT distinct c.id, c.user_id, c.layer_id, c.origin_type, c.timestamp FROM changeset c 
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
                var c = new Changeset(id, userID, layerID, origin, timestamp);
                ret.Add(c);
            }
            return ret;
        }
    }
}
