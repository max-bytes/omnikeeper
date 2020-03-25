﻿using LandscapePrototype.Entity;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ILayerModel
    {
        Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans);
        Task<LayerSet> BuildLayerSet(NpgsqlTransaction trans);

        Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans);
        Task<Layer> GetLayer(string layerName, NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans);
    }
}
