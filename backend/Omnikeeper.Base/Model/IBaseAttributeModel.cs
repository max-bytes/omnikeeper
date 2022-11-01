using Omnikeeper.Base.Entity;
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
        Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IList<(Guid ciid, string name, IAttributeValue value, Guid attributeID, Guid newAttributeID)> removes,
            string layerID, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public interface IBaseAttributeModel : IBaseAttributeMutationModel
    {
        Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, IAttributeSelection attributeSelection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling);
        IAsyncEnumerable<CIAttribute> GetAttributesNew(ICIIDSelection selection, IAttributeSelection attributeSelection, string layerID, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling);

        Task<IReadOnlySet<Guid>> GetCIIDsWithAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime);

        Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime);

        Task<IReadOnlyList<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans);
    }


    public interface IBaseAttributeRevisionistModel
    {
        Task<int> DeleteAllAttributes(ICIIDSelection ciidSelection, string layerID, IModelContext trans);
        Task<int> DeleteOutdatedAttributesOlderThan(string[] layerIDs, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime);
    }
}
