using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ICIModel
    {
        Task<Guid> CreateCI(NpgsqlTransaction trans, Guid id);

        Task<CIType> GetCITypeByID(string typeID, NpgsqlTransaction trans, DateTimeOffset? atTime);
        Task<CIType> GetTypeOfCI(Guid ciid, NpgsqlTransaction trans, DateTimeOffset? atTime);
        Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<CI> GetCI(Guid ciid, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string typeID);
        Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<Guid> CIIDs = null);
    }
}
