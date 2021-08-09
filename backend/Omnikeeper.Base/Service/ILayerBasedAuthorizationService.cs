using Omnikeeper.Base.Entity;
using System.Collections.Generic;

namespace Omnikeeper.Base.Service
{
    public interface ILayerBasedAuthorizationService
    {
        bool CanUserReadFromLayer(AuthenticatedUser user, Layer layer);
        bool CanUserReadFromLayer(AuthenticatedUser user, long layerID);
        bool CanUserReadFromAllLayers(AuthenticatedUser user, IEnumerable<long> layerIDs);

        bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer);
        bool CanUserWriteToLayer(AuthenticatedUser user, long layerID);
        bool CanUserWriteToAllLayers(AuthenticatedUser user, IEnumerable<long> layerIDs);
    }
}