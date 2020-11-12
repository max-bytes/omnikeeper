using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IBaseAttributeModel
    {
        Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime);
        /**
         * NOTE: GetAttribute() and GetFullBinaryAttribute() can also return removed attributes
         */
        Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime);
        Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime);

        // mutations
        Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans);
        Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans);
        Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans);
        Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, IModelContext trans);

    }
}
