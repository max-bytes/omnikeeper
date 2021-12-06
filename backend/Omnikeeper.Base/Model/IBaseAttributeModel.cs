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
    public interface IBaseAttributeMutationModel
    {
        Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
        Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);

        Task<(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes
            )> PrepareForBulkUpdate<F>(IBulkCIAttributeData<F> data, IModelContext trans);

        Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes,
            string layerID, DataOriginV1 origin, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public interface IBaseAttributeModel : IBaseAttributeMutationModel
    {
        Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime);

        Task<ISet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime);

        Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans);
    }


    public interface IBaseAttributeRevisionistModel
    {
        Task<int> DeleteAllAttributes(string layerID, IModelContext trans);
        Task<int> DeleteOutdatedAttributesOlderThan(string[] layerIDs, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime);
    }
}
