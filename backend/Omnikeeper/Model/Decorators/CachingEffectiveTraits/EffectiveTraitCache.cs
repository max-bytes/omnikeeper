using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    public class EffectiveTraitCache
    {
        // lock to allow concurrent access
        private readonly ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();

        // this is actually NOT an exact cache of which CIs have which traits
        // instead, what this structure contains is guaranteed to be a superset of CIs that fulfill a particular trait
        // in other words, if a CI is part of a set (inside a dictionary entry), it MAY have that trait, it's not guaranteed;
        // BUT, if a CI is NOT part of a set, it is guaranteed to NOT have that trait
        // NOTE: using LayerSet objects as keys relies on their HashCode to be well-implemented. 
        private IDictionary<string, IDictionary<LayerSet, ISet<Guid>>> trait2cisSuperset = new Dictionary<string, IDictionary<LayerSet, ISet<Guid>>>();

        public void FullUpdateTrait(string traitID, LayerSet layerset, ISet<Guid> ciidsHavingTrait)
        {
            rwl.EnterWriteLock();
            if (trait2cisSuperset.TryGetValue(traitID, out var d)) {
                d.AddOrUpdate(layerset, ciidsHavingTrait);
            } else
            {
                trait2cisSuperset[traitID] = new Dictionary<LayerSet, ISet<Guid>>()
                {
                    {layerset, ciidsHavingTrait }
                };
            }
            rwl.ExitWriteLock();
        }

        public void AddCIID(Guid ciid, string layerID)
        {
            rwl.EnterWriteLock();
            foreach (var (traitID, d) in trait2cisSuperset)
            {
                foreach(var (layerset, set) in d)
                {
                    if (layerset.Contains(layerID))
                    {
                        set.Add(ciid);
                    }
                }
            }
            rwl.ExitWriteLock();
        }
        public void AddCIIDs(IEnumerable<Guid> ciids, string layerID)
        {
            rwl.EnterWriteLock();
            foreach (var (traitID, d) in trait2cisSuperset)
            {
                foreach (var (layerset, set) in d)
                {
                    if (layerset.Contains(layerID))
                    {
                        set.UnionWith(ciids);
                    }
                }
            }
            rwl.ExitWriteLock();
        }

        internal void RemoveCIIDs(IEnumerable<Guid> ciids, string traitID, LayerSet layerSet)
        {
            rwl.EnterWriteLock();
            if (trait2cisSuperset.TryGetValue(traitID, out var d))
            {
                if (d.TryGetValue(layerSet, out var set))
                {
                    set.ExceptWith(ciids);
                } else
                {
                    // if the cache does not have this trait/layerset combination, ignore
                }
            } else
            {
                // if the cache does not have this trait/layerset combination, ignore
            }
            rwl.ExitWriteLock();
        }

        public void PurgeLayer(string layerID)
        {
            rwl.EnterWriteLock();
            foreach (var (traitID, d) in trait2cisSuperset)
            {
                var toRemove = d.Keys.Where(t => t.Contains(layerID));
                foreach (var r in toRemove)
                    d.Remove(r);
            }
            rwl.ExitWriteLock();
        }

        public void PurgeAll()
        {
            rwl.EnterWriteLock();
            trait2cisSuperset.Clear();
            rwl.ExitWriteLock();
        }

        // only when this method returns true does the out variable have significance, otherwise the cache can't answer this 
        public bool GetCIIDsHavingTrait(string traitID, LayerSet layerset, out ISet<Guid>? ciids)
        {
            rwl.EnterReadLock();
            try
            {
                if (trait2cisSuperset.TryGetValue(traitID, out var d))
                {
                    if (d.TryGetValue(layerset, out ciids))
                    {
                        return true;
                    }
                }
                ciids = null;
                return false;
            } finally
            {
                rwl.ExitReadLock();
            }
        }

        // only when this method returns true does the out variable have significance, otherwise the cache can't answer this 
        public bool DoesCIIDHaveTrait(Guid ciid, string traitID, LayerSet layerset, out bool hasIt)
        {
            rwl.EnterReadLock();
            try
            {
                if (trait2cisSuperset.TryGetValue(traitID, out var d))
                {
                    if (d.TryGetValue(layerset, out var ciids))
                    {
                        hasIt = ciids.Contains(ciid);
                        return true;
                    }
                }
                hasIt = false;
                return false;
            } finally
            {
                rwl.ExitReadLock();
            }
        }
    }
}

/*
idea for a postgres based cache:

CREATE EXTENSION btree_gin;

CREATE INDEX idx_test on public.et_cache USING GIN ("layerset", "trait_id");

insert into public.et_cache values ('{"testlayer01"}', 'host', gen_random_uuid());
insert into public.et_cache values ('{"testlayer01", "testlayer02"}', 'host', gen_random_uuid());
insert into public.et_cache values ('{"testlayer02"}', 'host', gen_random_uuid());
insert into public.et_cache values ('{"testlayer02"}', 'host', gen_random_uuid());

SET enable_seqscan TO off;

--EXPLAIN ANALYZE
SELECT * FROM public.et_cache WHERE layerset @> ARRAY['testlayer02'];

*/