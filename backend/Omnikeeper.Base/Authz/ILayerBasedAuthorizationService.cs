using Omnikeeper.Base.Entity;
using System.Collections.Generic;

namespace Omnikeeper.Base.Authz
{
    public interface ILayerBasedAuthorizationService
    {
        bool CanUserReadFromLayer(IAuthenticatedUser user, string layerID);
        bool CanUserReadFromAllLayers(IAuthenticatedUser user, IEnumerable<string> layerIDs);

        bool CanUserWriteToLayer(IAuthenticatedUser user, Layer layer);
        bool CanUserWriteToLayer(IAuthenticatedUser user, string layerID);
        bool CanUserWriteToAllLayers(IAuthenticatedUser user, IEnumerable<string> layerIDs);

        IEnumerable<LayerData> FilterReadableLayers(IAuthenticatedUser user, IEnumerable<LayerData> layers);
        IEnumerable<LayerData> FilterWritableLayers(IAuthenticatedUser user, IEnumerable<LayerData> layers);
    }
}