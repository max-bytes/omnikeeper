﻿using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IPredicateModel
    {
        public enum PredicateStateFilter
        {
            ActiveOnly,
            ActiveAndDeprecated,
            All,
            MarkedForDeletion
        }
        Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, PredicateState state, NpgsqlTransaction trans);
        Task<bool> TryToDelete(string id, NpgsqlTransaction trans);
        Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, DateTimeOffset? atTime, PredicateStateFilter stateFilter);
    }
}
