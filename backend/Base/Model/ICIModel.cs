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
        Task<Guid> CreateCI(NpgsqlTransaction trans, Guid id);
        Task<Guid> CreateCI(NpgsqlTransaction trans);

        Task<Guid> CreateCIWithType(string typeID, NpgsqlTransaction trans);

        Task<IEnumerable<Guid>> GetCIIDs(NpgsqlTransaction trans);

        Task<CIType> UpsertCIType(string typeID, AnchorState state, NpgsqlTransaction trans);

        Task<IEnumerable<CIType>> GetCITypes(NpgsqlTransaction trans, TimeThreshold atTime);

        Task<CIType> GetCITypeByID(string typeID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<CIType> GetTypeOfCI(Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime, string typeID);
        Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<string> typeIDs);

        Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs = null);
        Task<IEnumerable<CompactCI>> GetCompactCIs(LayerSet visibleLayers, NpgsqlTransaction trans, TimeThreshold atTime, IEnumerable<Guid> CIIDs = null);

        Task<IDictionary<Guid, string>> GetCINames(IEnumerable<Guid> ciids, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
