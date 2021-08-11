using Omnikeeper.Base.Entity;
using System.Collections.Generic;

namespace Omnikeeper.Base.Service
{
    public interface ILayerBasedAuthorizationService
    {
        bool CanUserReadFromLayer(AuthenticatedUser user, Layer layer);
        bool CanUserReadFromLayer(AuthenticatedUser user, string layerID);
        bool CanUserReadFromAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs);

        bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer);
        bool CanUserWriteToLayer(AuthenticatedUser user, string layerID);
        bool CanUserWriteToAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs);
    }
}