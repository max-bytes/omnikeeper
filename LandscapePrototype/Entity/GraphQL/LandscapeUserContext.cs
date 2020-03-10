using LandscapePrototype.Model;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeUserContext : Dictionary<string, object>
    {
        private readonly HttpContext httpContext;

        public LandscapeUserContext(HttpContext httpContext)
        {
            this.httpContext = httpContext;
        }

        public string Username
        {
            get
            {
                // TODO: check if this works or is a hack, with a magic string
                var s = httpContext.User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
                if (s == null)
                    return "Unknown user";
                return s;
            }
        }

        public DateTimeOffset TimeThreshold {
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
