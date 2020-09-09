﻿using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IBaseRelationModel
    {
        Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);

        // mutations
        Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
        Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
        Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID, RelationState state)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, NpgsqlTransaction trans);
    }
}
