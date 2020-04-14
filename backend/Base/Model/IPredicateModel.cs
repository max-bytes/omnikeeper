using Landscape.Base.Entity;
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
            All
        }
        Task<string> CreatePredicate(string id, string wordingFrom, string wordingTo, NpgsqlTransaction trans);
        Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, DateTimeOffset? atTime, PredicateStateFilter stateFilter);
    }
}
