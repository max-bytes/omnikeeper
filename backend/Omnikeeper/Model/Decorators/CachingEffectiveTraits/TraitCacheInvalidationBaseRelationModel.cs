﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    public class TraitCacheInvalidationBaseRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly EffectiveTraitCache cache;

        public TraitCacheInvalidationBaseRelationModel(IBaseRelationModel model, IMetaConfigurationModel metaConfigurationModel, EffectiveTraitCache cache)
        {
            this.model = model;
            this.metaConfigurationModel = metaConfigurationModel;
            this.cache = cache;
        }

        public async Task<IEnumerable<(Guid fromCIID, Guid toCIID, string predicateID)>> BulkReplaceRelations<F>(IBulkRelationData<F> data, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var changed = await model.BulkReplaceRelations(data, changesetProxy, origin, trans);
            if (!changed.IsEmpty())
            {
                if (await metaConfigurationModel.IsLayerPartOfMetaConfiguration(data.LayerID, trans))
                    cache.PurgeAll();
                else
                    cache.AddCIIDs(changed.SelectMany(t => new Guid[] { t.fromCIID, t.toCIID }), data.LayerID);
            }
            return changed;
        }

        public async Task<(Relation relation, bool changed)> InsertRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.InsertRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                if (await metaConfigurationModel.IsLayerPartOfMetaConfiguration(layerID, trans))
                    cache.PurgeAll();
                else
                    cache.AddCIIDs(new Guid[] { fromCIID, toCIID }, layerID);
            }
            return t;
        }

        public async Task<(Relation relation, bool changed)> RemoveRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
        {
            var t = await model.RemoveRelation(fromCIID, toCIID, predicateID, layerID, changesetProxy, origin, trans);
            if (t.changed)
            {
                if (await metaConfigurationModel.IsLayerPartOfMetaConfiguration(layerID, trans))
                    cache.PurgeAll();
                else
                    cache.AddCIIDs(new Guid[] { fromCIID, toCIID }, layerID);
            }
            return t;
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetRelation(fromCIID, toCIID, predicateID, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<IEnumerable<Relation>> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await model.GetRelations(rl, layerID, trans, atTime);
        }
    }
}
