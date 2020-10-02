using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ICIModel
    {
        public static readonly string NameAttribute = "__name";

        // TODO: should return an ISet instead
        Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans);
        Task<bool> CIIDExists(Guid id, NpgsqlTransaction trans);

        Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CI>> GetCIs(ICIIDSelection selection, long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime);

        // merged
        Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<MergedCI>> GetMergedCIs(ICIIDSelection selection, LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> GetCompactCIs(ICIIDSelection selection, LayerSet visibleLayers, NpgsqlTransaction trans, TimeThreshold atTime);
        // TODO: should return an ISet instead
        Task<IEnumerable<Guid>> GetCIIDsOfNonEmptyCIs(LayerSet layerset, NpgsqlTransaction trans, TimeThreshold timeThreshold);

        Task<Guid> CreateCI(Guid id, NpgsqlTransaction trans);
        Task<Guid> CreateCI(NpgsqlTransaction trans);
    }
}
