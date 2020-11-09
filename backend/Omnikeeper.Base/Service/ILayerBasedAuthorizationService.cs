using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface ILayerBasedAuthorizationService
    {
        bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer);
        bool CanUserWriteToLayer(AuthenticatedUser user, long layerID);
        bool CanUserWriteToLayers(AuthenticatedUser user, IEnumerable<long> writeLayerIDs);
        Task<IEnumerable<Layer>> GetWritableLayersForUser(IEnumerable<Claim> claims, ILayerModel layerModel, NpgsqlTransaction trans);
    }
}