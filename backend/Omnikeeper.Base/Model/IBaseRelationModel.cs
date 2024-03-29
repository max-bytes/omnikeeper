﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IBaseRelationModel
    {
        Task<IReadOnlyList<Relation>[]> GetRelations(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling); // TODO: remove generatedDataHandling?

        Task<IReadOnlyList<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans);

        Task<IReadOnlySet<string>> GetPredicateIDs(IRelationSelection rs, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling); // TODO: remove generatedDataHandling?

        Task<(bool changed, Guid changesetID)> BulkUpdate(
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts,
            IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid existingRelationID, Guid newRelationID, bool mask)> removes,
            string layerID, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public interface IBaseRelationRevisionistModel
    {
        Task<int> DeleteAllRelations(string layerID, IModelContext trans);
        Task<int> DeleteOutdatedRelationsOlderThan(string[] layerIDs, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime);
    }
}
