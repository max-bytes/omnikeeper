using Landscape.Base.Entity;
using Landscape.Base.Model;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public class RelationService
    {
        public static async Task<IEnumerable<(Relation relation, MergedCI toCI)>> GetMergedForwardRelationsAndToCIs(MergedCI ci, ICIModel ciModel, IRelationModel relationModel, NpgsqlTransaction trans)
        {
            var relations = await relationModel.GetMergedRelations(ci.Identity, false, ci.Layers, IRelationModel.IncludeRelationDirections.Forward, trans, ci.AtTime);
            var relationsToCIIDs = relations.Select(r => r.ToCIID).Distinct();
            var relationsToCIs = (await ciModel.GetMergedCIs(ci.Layers, true, trans, ci.AtTime, relationsToCIIDs)).ToDictionary(ci => ci.Identity);
            var relationsAndToCIs = relations.Select(r => (relation: r, toCI: relationsToCIs[r.ToCIID]));
            return relationsAndToCIs;
        }
    }
}
