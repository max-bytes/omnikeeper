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

        public static async Task<IEnumerable<CompactRelatedCI>> GetCompactRelatedCIs(Guid ciid, LayerSet layerset, ICIModel ciModel, IRelationModel relationModel, int? perPredicateLimit, IModelContext trans, TimeThreshold atTime)
        {
            var relations = await relationModel.GetMergedRelations(new RelationSelectionEitherFromOrTo(ciid), layerset, trans, atTime);

            // HACK: limit number per predicate type
            var predicateCounts = new Dictionary<string, int>();
            var limitedRelations = new List<MergedRelation>();
            if (perPredicateLimit.HasValue)
            {
                foreach (var r in relations)
                {
                    predicateCounts.TryGetValue(r.Relation.PredicateID, out int current);
                    predicateCounts[r.Relation.PredicateID] = current + 1;
                    if (current < perPredicateLimit.Value)
                        limitedRelations.Add(r);
                }
            }
            else
            {
                limitedRelations.AddRange(relations);
            }

            var relationTuples = limitedRelations.Select(r =>
            {
                var isForwardRelation = r.Relation.FromCIID == ciid;
                var relatedCIID = (isForwardRelation) ? r.Relation.ToCIID : r.Relation.FromCIID;
                return (relation: r, relatedCIID, isForwardRelation);
            });


            var relatedCIs = new List<CompactRelatedCI>();

            if (!relationTuples.IsEmpty())
            {
                var relatedCompactCIs = (await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(relationTuples.Select(t => t.relatedCIID).ToHashSet()), layerset, trans, atTime))
                    .ToDictionary(ci => ci.ID); // TODO: performance improvements
                foreach ((var relation, var relatedCIID, var isForwardRelation) in relationTuples)
                {
                    var predicateID = relation.Relation.PredicateID;
                    var changesetID = relation.Relation.ChangesetID;
                    if (relatedCompactCIs.TryGetValue(relatedCIID, out var ci)) // TODO: performance improvements
                        relatedCIs.Add(new CompactRelatedCI(ci, relation.Relation.ID, relation.Relation.FromCIID, relation.Relation.ToCIID, changesetID, relation.Relation.Origin, predicateID, isForwardRelation, relation.LayerStackIDs));
                }
            }

            return relatedCIs;
        }
    }
}
