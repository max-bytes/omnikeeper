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
        Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime);
        /**
         * NOTE: GetAttribute(), GetFullBinaryAttribute(),GetAttributesOfChangeset() can also return removed attributes
         */
        Task<CIAttribute?> GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans);

        Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<Guid>> FindCIIDsWithAttributeNameAndValue(string name, IAttributeValue value, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime);

        /**
         * NOTE: this does not return an entry for CIs that do not have a name specified (in that layer); in other words, it can return less than specified via the selection parameter
         */
        Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime);

        // mutations
        Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
        Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
        Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
    }

    public interface IBaseAttributeRevisionistModel
    {
        Task<int> DeleteAllAttributes(string layerID, IModelContext trans);
    }

    public static class BaseAttributeModelExtensions
    {
        public static async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(this IBaseAttributeModel baseAttributeModel, string nameValue, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
            => await baseAttributeModel.InsertAttribute(ICIModel.NameAttribute, new AttributeScalarValueText(nameValue), ciid, layerID, changesetProxy, origin, trans);
    }
}
