using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseRelationModel : IBaseRelationModel
    {
        private readonly IPartitionModel partitionModel;

        public static bool _USE_LATEST_TABLE = true;

        public BaseRelationModel(IPartitionModel partitionModel)
        {
            this.partitionModel = partitionModel;
        }

        private (string? whereClause, IEnumerable<NpgsqlParameter> parameters) Eval(IRelationSelection rl)
        {
            switch (rl)
            {
                case RelationSelectionFrom rsft:
                    return ("(from_ci_id = ANY(@from_ci_ids))", new[] { new NpgsqlParameter("from_ci_ids", rsft.FromCIIDs.ToArray()) });
                case RelationSelectionTo rst:
                    return ("(to_ci_id = ANY(@to_ci_ids))", new[] { new NpgsqlParameter("to_ci_ids", rst.ToCIIDs.ToArray()) });
                case RelationSelectionWithPredicate rsp:
                    return ("(predicate_id = @predicate_id)", new[] { new NpgsqlParameter("predicate_id", rsp.PredicateID) });
                case RelationSelectionAll _:
                    return (null, new NpgsqlParameter[0]);
                case RelationSelectionNone _:
                    return ("(1=0)", new NpgsqlParameter[0]);
                //case RelationSelectionOr or:
                //    var (whereClause, parameters) = or.inners.Select(t => Eval(t)).Aggregate((tPrev, tNew) => (tPrev.whereClause + " or " + tNew.whereClause, tPrev.parameters.Concat(tNew.parameters)));
                //    return ("(" + whereClause + ")", parameters);
                default:
                    throw new Exception("Invalid relation selection");
            }
        }

        // TODO: rework to use CTEs, like attributes use -> performs much better
        private async Task<NpgsqlCommand> CreateRelationCommand(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            var innerWhereClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            var (rlInnerWhereClause, rlParameters) = Eval(rl);
            if (rlInnerWhereClause != null)
                innerWhereClauses.Add(rlInnerWhereClause);
            parameters.AddRange(rlParameters);

            var innerWhereClause = string.Join(" AND ", innerWhereClauses);
            if (innerWhereClause == "") innerWhereClause = "1=1";

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                var query = $@"select id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id from relation_latest
                    where layer_id = ANY(@layer_ids) and ({innerWhereClause})";
                command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                foreach (var p in parameters)
                    command.Parameters.Add(p);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                command.Prepare();
            }
            else
            {
                var query = $@"select id, from_ci_id, to_ci_id, predicate_id, changeset_id from (
                select distinct on (from_ci_id, to_ci_id, predicate_id) id, from_ci_id, to_ci_id, predicate_id, removed, changeset_id, layer_id from relation 
                    where timestamp <= @time_threshold and ({innerWhereClause}) and layer_id = ANY(@layer_ids)
                    and partition_index >= @partition_index
                    order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST
                ) i where i.removed = false
                "; // TODO: remove order by layer_id, but consider not breaking indices first
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);
                command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                foreach (var p in parameters)
                    command.Parameters.Add(p);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_ids", layerIDs);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                command.Prepare();
            }
            return command;
        }


        private async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

            NpgsqlCommand command;
            if (atTime.IsLatest && _USE_LATEST_TABLE)
            {
                command = new NpgsqlCommand(@"select id, changeset_id from relation_latest
                where from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id and layer_id = @layer_id and predicate_id = @predicate_id
                LIMIT 1", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Prepare();
            }
            else
            {
                command = new NpgsqlCommand(@"select id, changeset_id from (select id, removed, changeset_id from relation where 
                timestamp <= @time_threshold AND from_ci_id = @from_ci_id AND to_ci_id = @to_ci_id and layer_id = @layer_id and predicate_id = @predicate_id 
                and partition_index >= @partition_index
                order by timestamp DESC NULLS LAST
                LIMIT 1) i where i.removed = false", trans.DBConnection, trans.DBTransaction);
                command.Parameters.AddWithValue("from_ci_id", fromCIID);
                command.Parameters.AddWithValue("to_ci_id", toCIID);
                command.Parameters.AddWithValue("predicate_id", predicateID);
                command.Parameters.AddWithValue("layer_id", layerID);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("partition_index", partitionIndex);
                command.Prepare();
            }
            using var dr = await command.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
                return null;

            command.Dispose();

            var id = dr.GetGuid(0);
            var changesetID = dr.GetGuid(1);

            return new Relation(id, fromCIID, toCIID, predicateID, changesetID);
        }

        public async Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rs, string[] layerIDs, IModelContext trans, TimeThreshold atTime)
        {
            var tmp = new Dictionary<string, List<Relation>>();
            using (var command = await CreateRelationCommand(rs, layerIDs, trans, atTime))
            {
                using var dr = await command.ExecuteReaderAsync();

                while (await dr.ReadAsync())
                {
                    var id = dr.GetGuid(0);
                    var fromCIID = dr.GetGuid(1);
                    var toCIID = dr.GetGuid(2);
                    var predicateID = dr.GetString(3);
                    var changesetID = dr.GetGuid(4);
                    var layerID = dr.GetString(5);

                    var relation = new Relation(id, fromCIID, toCIID, predicateID, changesetID);
                    if (tmp.TryGetValue(layerID, out var e))
                        e.Add(relation);
                    else
                        tmp.Add(layerID, new List<Relation>() { relation });
                }
            }

            var relations = new List<Relation>[layerIDs.Length];
            for (var i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                if (tmp.TryGetValue(layerID, out var t))
                    relations[i] = t;
                else
                    relations[i] = new List<Relation>();
            }

            return relations;
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            var ret = new List<Relation>();
            using var command = new NpgsqlCommand($@"
            select id, from_ci_id, to_ci_id, predicate_id FROM relation 
            where changeset_id = @changeset_id and removed = @removed
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("changeset_id", changesetID);
            command.Parameters.AddWithValue("removed", getRemoved);

            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            while (dr.Read())
            {
                var id = dr.GetGuid(0);
                var fromCIID = dr.GetGuid(1);
                var toCIID = dr.GetGuid(2);
                var predicateID = dr.GetString(3);

                var relation = new Relation(id, fromCIID, toCIID, predicateID, changesetID);
                ret.Add(relation);
            }
            return ret;
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            if (fromCIID == toCIID)
                throw new Exception("From and To CIID must not be the same!");
            if (predicateID.IsEmpty())
                throw new Exception("PredicateID must not be empty");
            IDValidations.ValidatePredicateIDThrow(predicateID);

            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation != null)
            {
                // same predicate already exists and is present
                return (currentRelation, false);
            }

            var id = Guid.NewGuid();
            var (_, changesetID) = await _BulkUpdate(
                new (Guid, Guid, string, Guid)[] { (fromCIID, toCIID, predicateID, id) },
                new (Guid, Guid, string, Guid)[0],
                layerID, origin, changesetProxy, trans);

            return (new Relation(id, fromCIID, toCIID, predicateID, changesetID), true);
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var currentRelation = await GetRelation(fromCIID, toCIID, predicateID, layerID, trans, changesetProxy.TimeThreshold);

            if (currentRelation == null)
            {
                // relation does not exist
                throw new Exception("Trying to remove relation that does not exist");
            }

            var id = Guid.NewGuid();

            var (_, changesetID) = await _BulkUpdate(
                new (Guid, Guid, string, Guid)[0],
                new (Guid, Guid, string, Guid)[] { (fromCIID, toCIID, predicateID, id) },
                layerID, origin, changesetProxy, trans);

            return (new Relation(id, fromCIID, toCIID, predicateID, changesetID), true);
        }


        // NOTE: this bulk operation DOES check if the relations that are inserted are "unique":
        // it is not possible to insert the "same" relation (same from_ciid, to_ciid, predicate_id and layer) multiple times
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            async Task<IEnumerable<Relation>[]> GetOutdatedRelationsFromCIAndPredicateScope(BulkRelationDataCIAndPredicateScope cp, string[] layerIDs, IModelContext trans, TimeThreshold timeThreshold)
            {
                var dLookup = cp.Relevant.ToLookup(dd => dd.thisCIID, dd => dd.predicateID);
                var relationSelection = (cp.Outgoing) ? RelationSelectionFrom.Build(cp.Relevant.Select(dd => dd.thisCIID).ToHashSet()) : RelationSelectionTo.Build(cp.Relevant.Select(dd => dd.thisCIID).ToHashSet());
                var allRelations = await GetRelations(relationSelection, layerIDs, trans, timeThreshold); // TODO: restrict to relevant predicateIDs at fetch point
                var outdatedRelations = allRelations[0].Where(r => dLookup[(cp.Outgoing) ? r.FromCIID : r.ToCIID].Contains(r.PredicateID));
                return new IEnumerable<Relation>[] { outdatedRelations };
            }

            var outdatedRelations = (data switch
            {
                BulkRelationDataPredicateScope p => (await GetRelations(RelationSelectionWithPredicate.Build(p.PredicateID), new string[] { data.LayerID }, trans, changesetProxy.TimeThreshold)),
                BulkRelationDataLayerScope l => (await GetRelations(RelationSelectionAll.Instance, new string[] { data.LayerID }, trans, changesetProxy.TimeThreshold)),
                BulkRelationDataCIAndPredicateScope cp => await GetOutdatedRelationsFromCIAndPredicateScope(cp, new string[] { cp.LayerID }, trans, changesetProxy.TimeThreshold),
                _ => throw new Exception("Unknown scope")
            }).SelectMany(r => r).ToDictionary(r => r.InformationHash);

            var actualInserts = new List<(Guid fromCIID, Guid toCIID, string predicateID, Guid newRelationID)>();
            foreach (var fragment in data.Fragments)
            {
                var fromCIID = data.GetFromCIID(fragment);
                var toCIID = data.GetToCIID(fragment);
                if (fromCIID == toCIID)
                    throw new Exception("From and To CIID must not be the same!");

                var predicateID = data.GetPredicateID(fragment);
                if (predicateID.IsEmpty())
                    throw new Exception("PredicateID must not be empty");
                IDValidations.ValidatePredicateIDThrow(predicateID);

                var informationHash = Relation.CreateInformationHash(fromCIID, toCIID, predicateID);
                // remove the current relation from the list of relations to remove
                outdatedRelations.Remove(informationHash, out var currentRelation);

                if (currentRelation != null)
                {
                    continue;
                }

                Guid relationID = Guid.NewGuid();
                actualInserts.Add((fromCIID, toCIID, predicateID, relationID));
            }



            // mask-based changes to inserts and removals
            // depending on mask-handling, calculate relations that are potentially "maskable" in below layers
            //var maskableRelationsInBelowLayers = new Dictionary<string, (Guid ciid, string name)>();
            //switch (maskHandling)
            //{
            //    case MaskHandlingForRemovalApplyMaskIfNecessary n:

            //        maskableRelationsInBelowLayers = (data switch
            //        {
            //            BulkCIAttributeDataLayerScope d => (d.NamePrefix.IsEmpty()) ?
            //                    (await GetAttributes(new AllCIIDsSelection(), AllAttributeSelection.Instance, n.ReadLayersBelowWriteLayer, trans, readTS)) :
            //                    (await GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection($"^{d.NamePrefix}"), n.ReadLayersBelowWriteLayer, trans, readTS)),
            //            BulkCIAttributeDataCIScope d =>
            //                await GetAttributes(SpecificCIIDsSelection.Build(d.CIID), AllAttributeSelection.Instance, n.ReadLayersBelowWriteLayer, trans: trans, atTime: readTS),
            //            BulkCIAttributeDataCIAndAttributeNameScope a =>
            //                await GetAttributes(SpecificCIIDsSelection.Build(a.RelevantCIs), NamedAttributesSelection.Build(a.RelevantAttributes), n.ReadLayersBelowWriteLayer, trans, readTS),
            //            _ => throw new Exception("Unknown scope")
            //        })
            //        .SelectMany(t => t.Values.SelectMany(tt => tt.Values))
            //        .GroupBy(t => t.InformationHash)
            //        .Where(g => !informationHashesToInsert.Contains(g.Key)) // if we are already inserting this attribute, we definitely do not want to mask it
            //        .ToDictionary(g => g.Key, g => (g.First().CIID, g.First().Name));
            //        break;
            //    case MaskHandlingForRemovalApplyNoMask _:
            //        // no operation necessary
            //        break;
            //    default:
            //        throw new Exception("Invalid mask handling");
            //}
            //// reduce the actual removes by looking at maskable attributes, replacing the removes with masks if necessary
            //foreach (var kv in maskableRelationsInBelowLayers)
            //{
            //    var ih = kv.Key;

            //    if (outdatedAttributes.TryGetValue(ih, out var outdatedAttribute))
            //    {
            //        // the attribute exists in the write-layer AND is actually outdated AND needs to be masked -> mask it, instead of removing it
            //        outdatedAttributes.Remove(ih);
            //        actualInserts.Add((outdatedAttribute.CIID, outdatedAttribute.Name, AttributeScalarValueMask.Instance, outdatedAttribute.ID, Guid.NewGuid()));
            //    }
            //    else
            //    {
            //        // the attribute exists only in the layers below -> mask it
            //        actualInserts.Add((kv.Value.ciid, kv.Value.name, AttributeScalarValueMask.Instance, null, Guid.NewGuid()));
            //    }
            //}




            // perform actual updates in bulk
            var removes = outdatedRelations.Values.Select(t => (t.FromCIID, t.ToCIID, t.PredicateID, Guid.NewGuid())).ToList();
            await _BulkUpdate(actualInserts, removes, data.LayerID, origin, changesetProxy, trans);

            // TODO: data (almost) is never used -> replace with a simpler return structure?
            return actualInserts.Select(r => (r.fromCIID, r.toCIID, r.predicateID))
                .Concat(outdatedRelations.Values.Select(r => (r.FromCIID, r.ToCIID, r.PredicateID)));
        }


        private async Task<(bool changed, Guid changesetID)> _BulkUpdate(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid newRelationID)> inserts,
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid newRelationID)> removes,
            string layerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (!inserts.IsEmpty() || !removes.IsEmpty())
            {
                Changeset changeset = await changesetProxy.GetChangeset(layerID, dataOrigin, trans);
                var partitionIndex = await partitionModel.GetLatestPartitionIndex(changesetProxy.TimeThreshold, trans);

                // historic
                using var writerHistoric = trans.DBConnection.BeginBinaryImport(@"COPY relation (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id, removed, ""timestamp"", partition_index) FROM STDIN (FORMAT BINARY)");
                foreach (var (fromCIID, toCIID, predicateID, newRelationID) in inserts)
                {
                    writerHistoric.StartRow();
                    writerHistoric.Write(newRelationID);
                    writerHistoric.Write(fromCIID);
                    writerHistoric.Write(toCIID);
                    writerHistoric.Write(predicateID);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(layerID);
                    writerHistoric.Write(false);
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }

                // remove outdated 
                foreach (var (fromCIID, toCIID, predicateID, newRelationID) in removes)
                {
                    writerHistoric.StartRow();
                    writerHistoric.Write(newRelationID);
                    writerHistoric.Write(fromCIID);
                    writerHistoric.Write(toCIID);
                    writerHistoric.Write(predicateID);
                    writerHistoric.Write(changeset.ID);
                    writerHistoric.Write(layerID);
                    writerHistoric.Write(true);
                    writerHistoric.Write(changeset.Timestamp, NpgsqlDbType.TimestampTz);
                    writerHistoric.Write(partitionIndex, NpgsqlDbType.TimestampTz);
                }
                writerHistoric.Complete();
                writerHistoric.Close();

                // latest
                // new inserts
                if (!inserts.IsEmpty())
                {
                    using var writerLatest = trans.DBConnection.BeginBinaryImport(@"COPY relation_latest (id, from_ci_id, to_ci_id, predicate_id, changeset_id, layer_id) FROM STDIN (FORMAT BINARY)");
                    foreach (var (fromCIID, toCIID, predicateID, newRelationID) in inserts)
                    {
                        writerLatest.StartRow();
                        writerLatest.Write(newRelationID);
                        writerLatest.Write(fromCIID);
                        writerLatest.Write(toCIID);
                        writerLatest.Write(predicateID);
                        writerLatest.Write(changeset.ID);
                        writerLatest.Write(layerID);
                    }
                    writerLatest.Complete();
                    writerLatest.Close();
                }

                // removals
                // we cannot do COPY commands here, but at least use CTEs to make the deletes perform better
                if (!removes.IsEmpty())
                {
                    // TODO: use string builder for perf instead?
                    var withClause = string.Join("",
                        "WITH to_delete(from_ci_id, to_ci_id, predicate_id, id) AS (VALUES ",
                        string.Join(",", removes.Select(r => $"('{r.fromCIID}'::uuid, '{r.toCIID}'::uuid, '{r.predicateID}', '{r.newRelationID}')")),
                        " )");

                    using var commandRemoveLatest = new NpgsqlCommand(@$"
                        {withClause}
                        DELETE FROM relation_latest r
                        USING to_delete t
                        WHERE r.from_ci_id = t.from_ci_id AND r.to_ci_id = t.to_ci_id AND r.predicate_id = t.predicate_id AND r.layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                    commandRemoveLatest.Parameters.AddWithValue("layer_id", layerID);
                    await commandRemoveLatest.ExecuteNonQueryAsync();
                }

                return (true, changeset.ID);
            }
            else
            {
                return (false, default);
            }
        }
    }
}
