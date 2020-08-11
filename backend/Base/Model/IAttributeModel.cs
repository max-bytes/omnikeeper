using Landscape.Base.Entity;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IAttributeModel : IBaseAttributeModel
    {
        Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
