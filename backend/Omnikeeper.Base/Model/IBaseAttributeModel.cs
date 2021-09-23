using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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
        Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection);
        /**
         * NOTE: GetFullBinaryAttribute(),GetAttributesOfChangeset() can also return removed attributes
         */
        Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans);

        Task<IDictionary<Guid, CIAttribute>[]> FindAttributesByFullName(string name, ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime);

        // mutations
        Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
        Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
        Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
    }

    public interface IBaseAttributeRevisionistModel
    {
        Task<int> DeleteAllAttributes(string layerID, IModelContext trans);
        Task<int> DeleteOutdatedAttributesOlderThan(string layerID, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime);
    }

    public static class BaseAttributeModelExtensions
    {
        public static async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(this IBaseAttributeModel baseAttributeModel, string nameValue, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
            => await baseAttributeModel.InsertAttribute(ICIModel.NameAttribute, new AttributeScalarValueText(nameValue), ciid, layerID, changesetProxy, origin, trans);
    }
}
