﻿using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class UsageDataAccumulator : IUsageDataAccumulator
    {
        private readonly ConcurrentQueue<(string username, DateTimeOffset timestamp, IEnumerable<(string elementType, string elementName, string layerID, UsageStatsOperation operation)> elements)> accumulator;

        public UsageDataAccumulator()
        {
            accumulator = new ConcurrentQueue<(string username, DateTimeOffset timestamp, IEnumerable<(string elementType, string elementName, string layerID, UsageStatsOperation operation)> elements)>();
        }

        public void Add(string username, DateTimeOffset timestamp, IEnumerable<(string elementType, string elementName, string layerID, UsageStatsOperation operation)> elements)
        {
            accumulator.Enqueue((username, timestamp, elements));
        }

        public void Flush(IModelContext trans)
        {
            using var writer = trans.DBConnection.BeginBinaryImport(@"COPY usage_stats (element_type, element_name, username, layer_id, operation, timestamp) FROM STDIN (FORMAT BINARY)");
            while (accumulator.TryDequeue(out var ds))
            {
                foreach (var (elementType, elementName, layerID, operation) in ds.elements)
                {
                    writer.StartRow();
                    writer.Write(elementType);
                    writer.Write(elementName);
                    writer.Write(ds.username);
                    writer.Write(layerID);
                    writer.Write(operation);
                    writer.Write(ds.timestamp.ToUniversalTime());
                }
            }
            writer.Complete();
            writer.Close();
        }

        public async Task<int> DeleteOlderThan(DateTimeOffset deleteThreshold, IModelContext trans)
        {
            using var _ = await trans.WaitAsync();
            var query = @"DELETE FROM public.usage_stats where ""timestamp"" < @delete_threshold";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("delete_threshold", deleteThreshold.ToUniversalTime());
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }
    }
}
