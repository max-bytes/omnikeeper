using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IRecursiveTraitWriteService
    {
        Task<(RecursiveTrait trait, bool changed)> InsertOrUpdate(
               string id, IEnumerable<TraitAttribute> requiredAttributes, IEnumerable<TraitAttribute>? optionalAttributes, IEnumerable<TraitRelation>? requiredRelations, IEnumerable<string>? requiredTraits,
               DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);

        Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
    }
}
