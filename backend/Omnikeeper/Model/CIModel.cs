using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class CIModel : ICIModel
    {
        private readonly IAttributeModel attributeModel;
        private readonly ICIIDModel ciidModel;

        public CIModel(IAttributeModel attributeModel, ICIIDModel ciidModel)
        {
            this.attributeModel = attributeModel;
            this.ciidModel = ciidModel;
        }

        public async Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold atTime)
        {
            var attributes = await attributeModel.GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), attributeSelection, layers, trans, atTime, GeneratedDataHandlingInclude.Instance);
            var cis =  BuildMergedCIs(attributes, layers, atTime);
            return cis.First();
        }


        public async Task<IEnumerable<MergedCI>> GetMergedCIs(ICIIDSelection selection, LayerSet layers, bool includeEmptyCIs, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold atTime)
        {
            var attributes = await attributeModel.GetMergedAttributes(selection, attributeSelection, layers, trans, atTime, GeneratedDataHandlingInclude.Instance);

            if (includeEmptyCIs)
                return await BuildMergedCIsIncludingEmptyCIs(attributes, selection, layers, trans, atTime);
            else
                return BuildMergedCIs(attributes, layers, atTime);
        }


        private string? GetNameFromAttributes(IDictionary<string, MergedCIAttribute> attributes)
        {
            if (attributes.TryGetValue(ICIModel.NameAttribute, out var nameA))
                return nameA.Attribute.Value.Value2String();
            return null; // TODO: we assume we can convert the name to a string, is this correct?
        }

        public IList<MergedCI> BuildMergedCIs(IDictionary<Guid, IDictionary<string, MergedCIAttribute>> attributes, LayerSet layers, TimeThreshold atTime)
        {
            var ret = new List<MergedCI>();
            foreach (var ga in attributes)
            {
                var att = ga.Value;
                var name = GetNameFromAttributes(att);
                ret.Add(new MergedCI(ga.Key, name, layers, atTime, att));
            }

            return ret;
        }

        public async Task<IEnumerable<MergedCI>> BuildMergedCIsIncludingEmptyCIs(IDictionary<Guid, IDictionary<string, MergedCIAttribute>> attributes, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = BuildMergedCIs(attributes, layers, atTime);

            // check which ci we already got and which are empty, add the empty ones
            var allSelectedCIIDs = await selection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
            var emptyCIIDs = allSelectedCIIDs.Except(attributes.Keys);
            foreach (var emptyCIID in emptyCIIDs)
            {
                ret.Add(new MergedCI(emptyCIID, null, layers, atTime, ImmutableDictionary<string, MergedCIAttribute>.Empty));
            }

            return ret;
        }

        public Guid CreateCIID() => Guid.NewGuid();

        public async Task<Guid> CreateCI(IModelContext trans) => await CreateCI(CreateCIID(), trans);
        public async Task<Guid> CreateCI(Guid id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"INSERT INTO ci (id) VALUES (@id)", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("id", id);
            command.Prepare();
            await command.ExecuteNonQueryAsync();
            return id;
        }
        public async Task BulkCreateCIs(IEnumerable<Guid> ids, IModelContext trans)
        {
            using var writer = trans.DBConnection.BeginBinaryImport(@"COPY ci (id) FROM STDIN (FORMAT BINARY)");
            foreach (var ciid in ids)
            {
                writer.StartRow();
                writer.Write(ciid);
            }
            await writer.CompleteAsync();
        }

        public async Task<IEnumerable<Guid>> GetCIIDs(IModelContext trans)
        {
            return await ciidModel.GetCIIDs(trans);
        }

        public async Task<bool> CIIDExists(Guid id, IModelContext trans)
        {
            return await ciidModel.CIIDExists(id, trans);
        }
    }
}
