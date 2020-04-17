﻿using Landscape.Base.Entity;
using LandscapeRegistry;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ICIModel
    {
        Task<CIType> GetCITypeByID(string typeID, NpgsqlTransaction trans);
        Task<CIType> GetTypeOfCI(string ciid, NpgsqlTransaction trans, DateTimeOffset? atTime);
        Task<MergedCI> GetMergedCI(string ciIdentity, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<CI> GetCI(string ciid, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<CI>> GetCIs(long layerID, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<MergedCI>> GetMergedCIsByType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string typeID);
        Task<IEnumerable<MergedCI>> GetMergedCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null);
    }
}
