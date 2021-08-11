﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils
{
    public static class PermissionUtils
    {
        public static string GetLayerReadPermission(Layer layer) => GetLayerReadPermission(layer.ID);
        public static string GetLayerWritePermission(Layer layer) => GetLayerWritePermission(layer.ID);

        public static string GetLayerReadPermission(string layerID) => $"ok.layer.{layerID}#read";
        public static string GetLayerWritePermission(string layerID) => $"ok.layer.{layerID}#write";

        public static string GetManagementPermission() => $"ok.management";

        public static async Task<ISet<string>> GetAllAvailablePermissions(ILayerModel layerModel, IModelContext trans)
        {
            var allLayers = await layerModel.GetLayers(trans);
            return new string[] { GetManagementPermission() }
                .Concat(allLayers.SelectMany(l => new string[] { GetLayerReadPermission(l), GetLayerWritePermission(l) }))
                .ToHashSet()
            ;
        }
    }
}
