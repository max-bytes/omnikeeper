using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omnikeeper.Base.Utils
{
    public static class PermissionUtils
    {
        public static string GetLayerReadPermission(Layer layer) => GetLayerReadPermission(layer.ID);
        public static string GetLayerWritePermission(Layer layer) => GetLayerWritePermission(layer.ID);

        public static string GetLayerReadPermission(long layerID) => $"ok.layer.{layerID}#read";
        public static string GetLayerWritePermission(long layerID) => $"ok.layer.{layerID}#write";

        public static string GetManagementPermission() => $"ok.management";
    }
}
