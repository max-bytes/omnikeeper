﻿using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITraitsProvider
    {
        Task<TraitSet> GetActiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold);
    }
}
