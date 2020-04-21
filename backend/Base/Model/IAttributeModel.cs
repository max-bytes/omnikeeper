using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IAttributeModel
    {
        interface IAttributeSelection
        {
            string WhereClause { get; }
            void AddParameters(NpgsqlParameterCollection p);
        }
        class SingleCIIDAttributeSelection : IAttributeSelection
        {
            public Guid CIID { get; }
            public SingleCIIDAttributeSelection(Guid ciid)
            {
                CIID = ciid;
            }
            public string WhereClause => "ci_id = @ci_id";
            public void AddParameters(NpgsqlParameterCollection p) => p.AddWithValue("ci_id", CIID);
        }
        class MultiCIIDsAttributeSelection : IAttributeSelection
        {
            public Guid[] CIIDs { get; }
            public MultiCIIDsAttributeSelection(Guid[] ciids)
            {
                CIIDs = ciids;
            }
            public string WhereClause => "ci_id = ANY(@ci_ids)";
            public void AddParameters(NpgsqlParameterCollection p) => p.AddWithValue("ci_ids", CIIDs);
        }
        class AllCIIDsAttributeSelection : IAttributeSelection
        {
            public string WhereClause => "1=1";
            public void AddParameters(NpgsqlParameterCollection p) { }
        }

        Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(IEnumerable<Guid> ciids, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<CIAttribute>> GetAttributes(IAttributeSelection selection, bool includeRemoved, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime);


        Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, long changesetID, NpgsqlTransaction trans);
        Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, long changesetID, NpgsqlTransaction trans);
        Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, bool includeRemoved, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime, Guid? ciid = null);
        Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);



        Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, long changesetID, NpgsqlTransaction trans);
    }
}
