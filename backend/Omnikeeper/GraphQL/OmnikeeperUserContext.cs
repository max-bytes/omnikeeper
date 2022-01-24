using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    public class OmnikeeperUserContext : Dictionary<string, object?>
    {
        public AuthenticatedUser User { get; private set; }
        public IServiceProvider ServiceProvider { get; }

        private class ScopedContext
        {
            public TimeThreshold? timeThreshold;
            public LayerSet? layerSet;

            public IDictionary<string, ScopedContext>? subContexts;
        }

        private readonly ScopedContext scopedContexts = new ScopedContext();

        private ScopedContext FindScopedContext(IList<object> contextPath, int skipFirstN, ScopedContext currentScopedContext)
        {
            if (skipFirstN >= contextPath.Count)
                return currentScopedContext;

            if (currentScopedContext.subContexts == null)
                return currentScopedContext;

            var t = contextPath[skipFirstN];
            if (t is string tt)
            {
                if (currentScopedContext.subContexts.TryGetValue(tt, out var subScopedContext))
                    return FindScopedContext(contextPath, skipFirstN + 1, subScopedContext);
                else
                    return currentScopedContext;
            }
            else
            {
                return FindScopedContext(contextPath, skipFirstN + 1, currentScopedContext);
            }
        }

        private ScopedContext FindOrCreateScopedContext(IList<object> contextPath, int skipFirstN, ScopedContext currentScopedContext)
        {
            if (skipFirstN >= contextPath.Count)
                return currentScopedContext;

            if (currentScopedContext.subContexts == null)
                currentScopedContext.subContexts = new Dictionary<string, ScopedContext>();

            var t = contextPath[skipFirstN];
            if (t is string tt)
            {
                if (currentScopedContext.subContexts.TryGetValue(tt, out var subScopedContext))
                    return FindOrCreateScopedContext(contextPath, skipFirstN + 1, subScopedContext);
                else
                {
                    currentScopedContext.subContexts[tt] = new ScopedContext();
                    return FindOrCreateScopedContext(contextPath, skipFirstN + 1, currentScopedContext.subContexts[tt]);
                }
            }
            else
            {
                return FindOrCreateScopedContext(contextPath, skipFirstN + 1, currentScopedContext);
            }
        }


        public OmnikeeperUserContext(AuthenticatedUser user, IServiceProvider sp)
        {
            User = user;
            ServiceProvider = sp;
        }

        public TimeThreshold GetTimeThreshold(IEnumerable<object> contextPath)
        {
            var foundContext = FindScopedContext(contextPath.ToList(), 0, scopedContexts);
            if (!foundContext.timeThreshold.HasValue)
                throw new Exception("TimeThreshold not set in current user context"); // throw exception, demand explicit setting
            else
                return foundContext.timeThreshold.Value;
        }

        public LayerSet GetLayerSet(IEnumerable<object> contextPath)
        {
            var foundContext = FindScopedContext(contextPath.ToList(), 0, scopedContexts);
            if (foundContext.layerSet == null)
                throw new Exception("LayerSet not set in current user context"); // throw exception, demand explicit setting
            else
                return foundContext.layerSet;
        }

        public IModelContext Transaction
        {
            get
            {
                TryGetValue("Transaction", out var t);
                if (t == null) throw new System.Exception("Expected transaction to be set");
                return (IModelContext)t;
            }
            set
            {
                this.AddOrUpdate("Transaction", value);
            }
        }

        internal OmnikeeperUserContext WithTransaction(Func<IModelContextBuilder, IModelContext> f)
        {
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            Transaction = f(modelContextBuilder);
            return this;
        }

        internal OmnikeeperUserContext WithTimeThreshold(TimeThreshold ts, IEnumerable<object> contextPath)
        {
            var foundContext = FindOrCreateScopedContext(contextPath.ToList(), 0, scopedContexts);
            foundContext.timeThreshold = ts;
            return this;
        }

        internal async Task<OmnikeeperUserContext> WithLayersetAsync(Func<IModelContext, Task<LayerSet>> f, IEnumerable<object> contextPath)
        {
            var foundContext = FindOrCreateScopedContext(contextPath.ToList(), 0, scopedContexts);
            var ls = await f(Transaction);
            foundContext.layerSet = ls;
            return this;
        }

        internal OmnikeeperUserContext WithLayerset(LayerSet ls, IEnumerable<object> contextPath)
        {
            var foundContext = FindOrCreateScopedContext(contextPath.ToList(), 0, scopedContexts);
            foundContext.layerSet = ls;
            return this;
        }

        internal void CommitAndStartNewTransaction(Func<IModelContextBuilder, IModelContext> f)
        {
            Transaction.Commit();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            Transaction = f(modelContextBuilder);
        }
    }
}
