﻿using Landscape.Base.Entity;
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
        Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changeset, NpgsqlTransaction trans);
        Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, NpgsqlTransaction trans);

        Task<int> ArchiveOutdatedAttributesOlderThan(DateTimeOffset threshold, long layerID, NpgsqlTransaction trans);
    }
}
