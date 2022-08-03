using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    public class OmnikeeperUserContext : Dictionary<string, object?>, IDisposable
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

        public IChangesetProxy ChangesetProxy
        {
            get
            {
                TryGetValue("ChangesetProxy", out var t);
                if (t == null) throw new System.Exception("Expected ChangesetProxy to be set");
                return (IChangesetProxy)t;
            }
            private set
            {
                this.AddOrUpdate("ChangesetProxy", value);
            }
        }

        public MultiMutationData? MultiMutationData
        {
            get
            {
                if (TryGetValue("MultiMutationData", out var t))
                    return t as MultiMutationData;
                return null;
            }
            set
            {
                this.AddOrUpdate("MultiMutationData", value);
            }
        }

        internal OmnikeeperUserContext WithTransaction(Func<IModelContextBuilder, IModelContext> f)
        {
            if (!ContainsKey("Transaction"))
            {
                var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
                Transaction = f(modelContextBuilder);
            }
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

        internal OmnikeeperUserContext WithChangesetProxy(IChangesetModel changesetModel, IEnumerable<object> contextPath)
        {
            if (!ContainsKey("ChangesetProxy"))
            {
                // NOTE: this is slightly incorrect, because we use the timeThreshold that is bound to the (current) contextPath
                // later parts may use different time thresholds, but the changesetProxy will stay the same and will keep this first timeThreshold;
                // but making the changesetProxy scoped (like TimeThreshold is) would not work either, because then it would not be able to
                // properly handle multiple mutations per request
                var changesetProxy = new ChangesetProxy(User.InDatabase, GetTimeThreshold(contextPath), changesetModel);
                ChangesetProxy = changesetProxy;
            }
            return this;
        }

        internal void CommitAndStartNewTransactionIfLastMutation(Func<IModelContextBuilder, IModelContext> f)
        {
            if (MultiMutationData != null && MultiMutationData.IsLastMutation)
            {
                Transaction.Commit();
                var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
                Transaction = f(modelContextBuilder);
            }
        }

        public void Dispose()
        {
            if (TryGetValue("Transaction", out var t))
            {
                if (t is IModelContext mc)
                    mc.Dispose();
            }
        }
    }
}
