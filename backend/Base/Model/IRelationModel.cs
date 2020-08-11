﻿using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IRelationModel
    {
        Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, bool includeRemoved, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);

        // merged
        Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, bool includeRemoved, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);

        // mutations
        Task<bool> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
        Task<Relation> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
        Task<Relation> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
    }
}
