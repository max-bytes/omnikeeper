﻿using Npgsql;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public static class RebuildLatestTablesService
    {
        public static async Task RebuildLatestAttributesTable(IPartitionModel partitionModel, ILayerModel layerModel, IModelContext trans)
        {
            // TODO: this process could be done much faster by doing a direct database->database copy

            var atTime = TimeThreshold.BuildLatest();

            var partitionIndex = await partitionModel.GetLatestPartitionIndex(atTime, trans);

            using var commandTruncate = new NpgsqlCommand($@"truncate table attribute_latest", trans.DBConnection, trans.DBTransaction);
            await commandTruncate.ExecuteNonQueryAsync();

            foreach (var layer in await layerModel.GetLayers(trans))
            {
                using var commandGet = new NpgsqlCommand($@"
                    select distinct on(ci_id, name) state, id, name, ci_id, type, value_text, value_binary, value_control, changeset_id, timestamp FROM attribute 
                    where timestamp <= @time_threshold and layer_id = @layer_id and partition_index >= @partition_index
                    order by ci_id, name, timestamp DESC NULLS LAST
                    ", trans.DBConnection, trans.DBTransaction);
                commandGet.Parameters.AddWithValue("layer_id", layer.ID);
                commandGet.Parameters.AddWithValue("time_threshold", atTime.Time);
                commandGet.Parameters.AddWithValue("partition_index", partitionIndex);

                commandGet.Prepare();

                var attributes = new List<(CIAttribute attribute, DateTime timestamp)>();
                using (var dr = await commandGet.ExecuteReaderAsync())
                {
                    commandGet.Dispose();

                    while (dr.Read())
                    {
                        var state = dr.GetFieldValue<AttributeState>(0);
                        if (state != AttributeState.Removed)
                        {
                            var id = dr.GetGuid(1);
                            var name = dr.GetString(2);
                            var CIID = dr.GetGuid(3);
                            var type = dr.GetFieldValue<AttributeValueType>(4);
                            var valueText = dr.GetString(5);
                            var valueBinary = dr.GetFieldValue<byte[]>(6);
                            var valueControl = dr.GetFieldValue<byte[]>(7);
                            var av = AttributeValueBuilder.Unmarshal(valueText, valueBinary, valueControl, type, false);
                            var changesetID = dr.GetGuid(8);
                            var timestamp = dr.GetDateTime(9);

                            var att = new CIAttribute(id, name, CIID, av, state, changesetID);
                            attributes.Add((att, timestamp));
                        }
                    }
                }

                // TODO: improve performance, consider using COPY
                foreach (var (attribute, timestamp) in attributes)
                {
                    using var commandInsert = new NpgsqlCommand(@"
                        INSERT INTO attribute_latest (id, name, ci_id, type, value_text, value_binary, value_control, layer_id, state, ""timestamp"", changeset_id) 
                        VALUES (@id, @name, @ci_id, @type, @value_text, @value_binary, @value_control, @layer_id, @state, @timestamp, @changeset_id)", trans.DBConnection, trans.DBTransaction);
                    var id = Guid.NewGuid();
                    var (valueText, valueBinary, valueControl) = AttributeValueBuilder.Marshal(attribute.Value);
                    commandInsert.Parameters.AddWithValue("id", id);
                    commandInsert.Parameters.AddWithValue("name", attribute.Name);
                    commandInsert.Parameters.AddWithValue("ci_id", attribute.CIID);
                    commandInsert.Parameters.AddWithValue("type", attribute.Value.Type);
                    commandInsert.Parameters.AddWithValue("value_text", valueText);
                    commandInsert.Parameters.AddWithValue("value_binary", valueBinary);
                    commandInsert.Parameters.AddWithValue("value_control", valueControl);
                    commandInsert.Parameters.AddWithValue("layer_id", layer.ID);
                    commandInsert.Parameters.AddWithValue("state", attribute.State);
                    commandInsert.Parameters.AddWithValue("timestamp", timestamp);
                    commandInsert.Parameters.AddWithValue("changeset_id", attribute.ChangesetID);
                    await commandInsert.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
