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

        Task<Guid> CreateCI(NpgsqlTransaction trans, Guid id);
        Task<Guid> CreateCI(NpgsqlTransaction trans);

        Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans);
        Task<IEnumerable<Guid>> GetCIIDsOfNonEmptyCIs(LayerSet layerset, NpgsqlTransaction trans, TimeThreshold timeThreshold);

        Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs);
        Task<IEnumerable<CompactCI>> GetCompactCIs(LayerSet visibleLayers, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs = null);

        Task<bool> CIIDExists(Guid id, NpgsqlTransaction trans);
    }
}
