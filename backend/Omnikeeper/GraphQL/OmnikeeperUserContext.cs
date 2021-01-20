using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL
{
    public class OmnikeeperUserContext : Dictionary<string, object?>
    {
        public AuthenticatedUser User { get; private set; }

        public OmnikeeperUserContext(AuthenticatedUser user)
        {
            User = user;
        }

        public TimeThreshold TimeThreshold
        {
            get
            {
                TryGetValue("TimeThreshold", out var ls);
                if (ls == null) return TimeThreshold.BuildLatest();
                return (TimeThreshold)ls;
            }
            set
            {
                Add("TimeThreshold", value);
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

        public LayerSet? LayerSet
        {
            get
            {
                TryGetValue("LayerSet", out var ls);
                if (ls == null) return null;
                return (LayerSet)ls;
            }
            set
            {
                Add("LayerSet", value);
            }
        }
    }
}
