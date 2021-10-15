﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IEffectiveTraitModel
    {
        Task<IEnumerable<EffectiveTrait>> GetEffectiveTraitsForCI(IEnumerable<ITrait> traits, MergedCI ci, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        Task<EffectiveTrait?> GetEffectiveTraitForCI(MergedCI ci, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);

        // TODO: unused? remove?
        Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsWithTraitAttributeValue(ITrait trait, string traitAttributeIdentifier, IAttributeValue value, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
    }
}
