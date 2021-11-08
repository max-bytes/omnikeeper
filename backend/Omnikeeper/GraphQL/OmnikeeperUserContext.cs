using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    public class OmnikeeperUserContext : Dictionary<string, object?>
    {
        public AuthenticatedUser User { get; private set; }
        public IServiceProvider ServiceProvider { get; }

        public OmnikeeperUserContext(AuthenticatedUser user, IServiceProvider sp)
        {
            User = user;
            ServiceProvider = sp;
        }

        public TimeThreshold TimeThreshold
        {
            get
            {
                CheckDisabledThrow();

                TryGetValue("TimeThreshold", out var ls);
                if (ls == null) return TimeThreshold.BuildLatest();
                return (TimeThreshold)ls;
            }
            set
            {
                CheckDisabledThrow();

                Add("TimeThreshold", value);
            }
        }

        public bool PartlyDisabled
        {
            get
            {
                TryGetValue("Disabled", out var disabled);
                if (disabled == null) return false;
                return (bool)disabled;
            }
            set
            {
                Add("Disabled", value);
            }
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

        public LayerSet LayerSet
        {
            get
            {
                CheckDisabledThrow();

                TryGetValue("LayerSet", out var ls);
                if (ls == null) throw new System.Exception("Expected layerset to be set");
                return (LayerSet)ls;
            }
            set
            {
                CheckDisabledThrow();

                Add("LayerSet", value);
            }
        }

        internal OmnikeeperUserContext WithTransaction(Func<IModelContextBuilder, IModelContext> f)
        {
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            Transaction = f(modelContextBuilder);
            return this;
        }

        internal OmnikeeperUserContext WithTimeThreshold(Func<TimeThreshold> f)
        {
            TimeThreshold = f();
            return this;
        }

        internal async Task<OmnikeeperUserContext> WithLayerset(Func<IModelContext, Task<LayerSet?>> f)
        {
            var ls = await f(Transaction);
            if (ls != null)
                LayerSet = ls;
            return this;
        }

        internal void CommitAndStartNewTransaction(Func<IModelContextBuilder, IModelContext> f)
        {
            Transaction.Commit();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            Transaction = f(modelContextBuilder);
        }

        private void CheckDisabledThrow()
        {
            if (PartlyDisabled) throw new Exception("Cannot use UserContext in this setting, it was disabled");
        }
    }
}
