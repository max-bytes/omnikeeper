using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omnikeeper.Base.Utils
{
    public static class PermissionUtils
    {
        public static bool CanWriteToLayer(IEnumerable<string> permissions, Layer layer)
        {
            return CanWriteToLayer(permissions, layer.ID);
        }

        public static bool CanWriteToLayer(IEnumerable<string> permissions, long layerID)
        {
            var toCheck = GetLayerWritePermission(layerID);
            return permissions.Contains(toCheck);
        }

        public static string GetLayerWritePermission(Layer layer)
        {
            return GetLayerWritePermission(layer.ID);
        }

        public static string GetLayerWritePermission(long layerID)
        {
            return $"ok.layer.{layerID}#write";
        }
    }
}
