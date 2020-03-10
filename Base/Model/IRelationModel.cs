using LandscapePrototype;
using LandscapePrototype.Entity;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IRelationModel
    {
        Task<IEnumerable<Relation>> GetRelationsWithPredicate(LayerSet layerset, bool includeRemoved, string predicate, NpgsqlTransaction trans, DateTimeOffset? timeThreshold = null);
    }
}
