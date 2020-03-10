using LandscapePrototype;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ICIModel
    {
        Task<CI> GetCI(string ciIdentity, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<CI>> GetCIsWithType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string type);
        Task<IEnumerable<CI>> GetCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null);
        Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, string ciid, long changesetID, NpgsqlTransaction trans);
    }
}
