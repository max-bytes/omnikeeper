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
        Task<CI> GetFullCI(string ciIdentity, LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime);
        Task<IEnumerable<CI>> GetFullCIsWithType(LayerSet layers, NpgsqlTransaction trans, DateTimeOffset atTime, string type);
        Task<IEnumerable<CI>> GetFullCIs(LayerSet layers, bool includeEmptyCIs, NpgsqlTransaction trans, DateTimeOffset atTime, IEnumerable<string> CIIDs = null);
        Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, string ciid, long changesetID, NpgsqlTransaction trans);
        Task<CIAttribute> RemoveAttribute(string name, long layerID, string ciid, long changesetID, NpgsqlTransaction trans);
        Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, bool includeRemoved, long layerID, NpgsqlTransaction trans, DateTimeOffset atTime);

        Task<bool> BulkReplaceAttributes(BulkCIAttributeData data, long changesetID, NpgsqlTransaction trans);
    }
}
