using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public class RelationService
    {
        public static async Task<IEnumerable<(Relation relation, MergedCI toCI)>> GetMergedForwardRelationsAndToCIs(
            Guid ciid, LayerSet layers, ICIModel ciModel, IRelationModel relationModel, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var relations = await relationModel.GetMergedRelations(ciid, false, layers, IRelationModel.IncludeRelationDirections.Forward, trans, atTime);
            var relationsToCIIDs = relations.Select(r => r.ToCIID).Distinct();
            var relationsToCIs = (await ciModel.GetMergedCIs(layers, true, trans, atTime, relationsToCIIDs)).ToDictionary(ci => ci.ID);
            var relationsAndToCIs = relations.Select(r => (relation: r, toCI: relationsToCIs[r.ToCIID]));
            return relationsAndToCIs;
        }
    }
}
