﻿using Landscape.Base.Entity;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IAttributeModel
    {
        Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime);

        Task<IEnumerable<CIAttribute>> FindAttributesByName(string like, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);

        // merged
        Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, bool includeRemoved, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);

        // mutations
        Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<CIAttribute> InsertCINameAttribute(string nameValue, long layerID, Guid ciid, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, NpgsqlTransaction trans);

    }
}
