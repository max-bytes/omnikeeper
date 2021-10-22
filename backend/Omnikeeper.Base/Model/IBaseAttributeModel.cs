﻿using Omnikeeper.Base.Entity;
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
        Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans);
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

    public static class BaseAttributeModelExtensions
    {
        public static async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(this IBaseAttributeMutationModel baseAttributeModel, string nameValue, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
            => await baseAttributeModel.InsertAttribute(ICIModel.NameAttribute, new AttributeScalarValueText(nameValue), ciid, layerID, changesetProxy, origin, trans);
    }
}
