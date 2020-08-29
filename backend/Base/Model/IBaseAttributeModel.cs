using Landscape.Base.Entity;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IBaseAttributeModel
    {
        Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<CIAttribute> GetAttribute(string name, Guid ciid, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);

        // mutations
        Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<CIAttribute> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<CIAttribute> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, NpgsqlTransaction trans);

    }
}
