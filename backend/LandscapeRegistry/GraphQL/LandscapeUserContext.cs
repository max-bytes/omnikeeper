using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Collections.Generic;

namespace LandscapeRegistry.GraphQL
{
    public class LandscapeUserContext : Dictionary<string, object>
    {
        public User User { get; private set; }

        public LandscapeUserContext(User user)
        {
            User = user;
        }

        public DateTimeOffset TimeThreshold
        {
            get
            {
                TryGetValue("TimeThreshold", out var ls);
                if (ls == null) return default;
                return (DateTimeOffset)ls;
            }
            set
            {
                Add("TimeThreshold", value);
            }
        }

        public NpgsqlTransaction Transaction
        {
            get
            {
                TryGetValue("Transaction", out var t);
                if (t == null) return default;
                return (NpgsqlTransaction)t;
            }
            set
            {
                Add("Transaction", value);
            }
        }

        public LayerSet LayerSet
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
