using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public class RelationService
    {
        //public static async Task<IEnumerable<CompactRelatedCI>> GetCompactRelatedCIs(Guid ciid, LayerSet layerset, ICIModel ciModel, IRelationModel relationModel, IModelContext trans, TimeThreshold atTime)
        //{
        //    var fromRelations = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid), layerset, trans, atTime);
        //    var toRelations = await relationModel.GetMergedRelations(new RelationSelectionTo(ciid), layerset, trans, atTime);

        //    var relationTuples = fromRelations.Select(r => (relation: r, relatedCIID: r.Relation.ToCIID)).Concat(
        //            toRelations.Select(r => (relation: r, relatedCIID: r.Relation.FromCIID))
        //        );

        //    var relatedCIs = new List<CompactRelatedCI>();

        //    if (!relationTuples.IsEmpty())
        //    {
        //        var relatedCompactCIs = (await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(relationTuples.Select(t => t.relatedCIID).ToHashSet()), layerset, trans, atTime))
        //            .ToDictionary(ci => ci.ID); // TODO: performance improvements
        //        foreach ((var relation, var relatedCIID) in relationTuples)
        //        {
        //            var predicateID = relation.Relation.PredicateID;
        //            var changesetID = relation.Relation.ChangesetID;
        //            if (relatedCompactCIs.TryGetValue(relatedCIID, out var ci)) // TODO: performance improvements
        //                relatedCIs.Add(new CompactRelatedCI(ci, relation.Relation.ID, relation.Relation.FromCIID, relation.Relation.ToCIID, changesetID, predicateID, relation.LayerStackIDs));
        //        }
        //    }

        //    return relatedCIs;
        //}
    }
}
