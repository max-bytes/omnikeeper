using Npgsql;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public static class RebuildLatestTablesService
    {
        // TODO: turn into proper automated test and compare _latest with _historic after lots of operations
        public static async Task _ValidateLatestAttributesTable(ILayerModel layerModel, IModelContext trans)
        {
            var atTime = TimeThreshold.BuildLatest();

            foreach (var layer in await layerModel.GetLayers(trans))
            {
                using var commandGetHistoric = new NpgsqlCommand($@"
                    select distinct on(ci_id, name) removed, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id FROM attribute 
                    where timestamp <= @time_threshold and layer_id = @layer_id
                    order by ci_id, name, timestamp DESC NULLS LAST
                    ", trans.DBConnection, trans.DBTransaction);
                commandGetHistoric.Parameters.AddWithValue("layer_id", layer.ID);
                commandGetHistoric.Parameters.AddWithValue("time_threshold", atTime.Time.ToUniversalTime());
                commandGetHistoric.Prepare();

                var attributesFromHistoric = new List<CIAttribute>();
                using (var dr = await commandGetHistoric.ExecuteReaderAsync())
                {
                    commandGetHistoric.Dispose();

                    while (dr.Read())
                    {
                        var removed = dr.GetBoolean(0);
                        if (!removed)
                        {
                            var id = dr.GetGuid(1);
                            var name = dr.GetString(2);
                            var CIID = dr.GetGuid(3);
                            var type = dr.GetFieldValue<AttributeValueType>(4);
                            var valueText = dr.GetString(5);
                            var valueBinary = dr.GetFieldValue<byte[]>(6);
                            var valueControl = dr.GetFieldValue<byte[]>(7);
                            var av = AttributeValueHelper.Unmarshal(valueText, valueBinary, valueControl, type, false);
                            var changesetID = dr.GetGuid(8);

                            var att = new CIAttribute(id, name, CIID, av, changesetID);
                            attributesFromHistoric.Add(att);
                        }
                    }
                }

                using var commandGetLatest = new NpgsqlCommand($@"
                select id, name, ci_id, type, value_text, value_binary, value_control, changeset_id FROM attribute_latest
                where layer_id = @layer_id", trans.DBConnection, trans.DBTransaction);
                commandGetLatest.Parameters.AddWithValue("layer_id", layer.ID);
                commandGetLatest.Prepare();

                var attributesFromLatest = new List<CIAttribute>();
                using (var dr = await commandGetLatest.ExecuteReaderAsync())
                {
                    commandGetLatest.Dispose();

                    while (dr.Read())
                    {
                        var id = dr.GetGuid(0);
                        var name = dr.GetString(1);
                        var CIID = dr.GetGuid(2);
                        var type = dr.GetFieldValue<AttributeValueType>(3);
                        var valueText = dr.GetString(4);
                        var valueBinary = dr.GetFieldValue<byte[]>(5);
                        var valueControl = dr.GetFieldValue<byte[]>(6);
                        var av = AttributeValueHelper.Unmarshal(valueText, valueBinary, valueControl, type, false);
                        var changesetID = dr.GetGuid(7);

                        var att = new CIAttribute(id, name, CIID, av, changesetID);
                        attributesFromLatest.Add(att);
                    }
                }

                var diffCount = attributesFromLatest.Count - attributesFromHistoric.Count;
                if (diffCount != 0)
                    Console.WriteLine($"Diff count: {diffCount}");

                foreach (var al in attributesFromLatest)
                {
                    var ah = attributesFromHistoric.FirstOrDefault(ah => ah.ID == al.ID);
                    if (ah == null)
                        Console.WriteLine($"Diff!");
                    else if (ah.CIID != al.CIID)
                        Console.WriteLine($"Diff!");
                    else if (ah.Name != al.Name)
                        Console.WriteLine($"Diff!");
                }
            }
        }

        public static async Task RebuildLatestAttributesTable(bool skipIfNonEmpty, ILayerModel layerModel, IModelContext trans)
        {
            if (skipIfNonEmpty)
            {
                using var commandCheckNonEmpty = new NpgsqlCommand(@"SELECT CASE 
                 WHEN EXISTS (SELECT * FROM attribute_latest LIMIT 1) THEN 1
                     ELSE 0
                   END", trans.DBConnection, trans.DBTransaction);
                var nonEmptyInt = ((int?)await commandCheckNonEmpty.ExecuteScalarAsync())!.Value;
                if (nonEmptyInt != 0)
                {
                    return;
                }
            }

            var atTime = TimeThreshold.BuildLatest();

            // truncate
            using var commandTruncate = new NpgsqlCommand($@"truncate table attribute_latest", trans.DBConnection, trans.DBTransaction);
            await commandTruncate.ExecuteNonQueryAsync();

            // rebuild
            foreach (var layer in await layerModel.GetLayers(trans))
            {
                var query = @"
                    insert into attribute_latest (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, ""timestamp"", changeset_id)
                    (
                        select id, name, ci_id, type, value_text, value_binary, value_control, layer_id, ""timestamp"", changeset_id from (
                            select distinct on(ci_id, name, layer_id) removed, id, name, ci_id, type, value_text, value_binary, value_control, layer_id, ""timestamp"", changeset_id FROM attribute 
                            where timestamp <= @time_threshold and layer_id = @layer_id
                            order by ci_id, name, layer_id, timestamp DESC NULLS LAST
                        ) i where i.removed = false
                    )
                ";
                using var commandBuild = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                commandBuild.Parameters.AddWithValue("layer_id", layer.ID);
                commandBuild.Parameters.AddWithValue("time_threshold", atTime.Time.ToUniversalTime());
                await commandBuild.ExecuteNonQueryAsync();
            }
        }

        public static async Task RebuildlatestRelationsTable(bool skipIfNonEmpty, ILayerModel layerModel, IModelContext trans)
        {
            if (skipIfNonEmpty)
            {
                using var commandCheckNonEmpty = new NpgsqlCommand(@"SELECT CASE 
                 WHEN EXISTS (SELECT * FROM relation_latest LIMIT 1) THEN 1
                     ELSE 0
                   END", trans.DBConnection, trans.DBTransaction);
                var nonEmptyInt = (int?)await commandCheckNonEmpty.ExecuteScalarAsync();
                if (nonEmptyInt != 0)
                {
                    return;
                }
            }

            var atTime = TimeThreshold.BuildLatest();

            // truncate
            using var commandTruncate = new NpgsqlCommand($@"truncate table relation_latest", trans.DBConnection, trans.DBTransaction);
            await commandTruncate.ExecuteNonQueryAsync();

            // rebuild
            foreach (var layer in await layerModel.GetLayers(trans))
            {
                var query = @"
                    insert into relation_latest (id, from_ci_id, to_ci_id, predicate_id, changeset_id, timestamp, layer_id)
                    (
                        select id, from_ci_id, to_ci_id, predicate_id, changeset_id, timestamp, layer_id from (
                            select distinct on(from_ci_id, to_ci_id, predicate_id, layer_id) removed, id, from_ci_id, to_ci_id, predicate_id, changeset_id, timestamp, layer_id from relation
                            where timestamp <= @time_threshold and layer_id = @layer_id
                            order by from_ci_id, to_ci_id, predicate_id, layer_id, timestamp DESC NULLS LAST
                        ) i where i.removed = false
                    )
                ";
                using var commandBuild = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
                commandBuild.Parameters.AddWithValue("layer_id", layer.ID);
                commandBuild.Parameters.AddWithValue("time_threshold", atTime.Time.ToUniversalTime());
                await commandBuild.ExecuteNonQueryAsync();
            }
        }

    }
}
