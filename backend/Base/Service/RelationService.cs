using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace Landscape.Base.Service
{
    public class RelationService
    {
        public static async Task<ILookup<string, MergedRelatedCI>> GetMergedRelatedCIs(
            Guid ciid, LayerSet layers, ICIModel ciModel, IRelationModel relationModel, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var relations = await relationModel.GetMergedRelations(new RelationSelectionEitherFromOrTo(ciid), layers, trans, atTime);
            var relationsOtherCIIDs = relations.Select(r => (r.Relation.FromCIID == ciid) ? r.Relation.ToCIID : r.Relation.FromCIID).Distinct();
            if (relationsOtherCIIDs.IsEmpty()) return new List<MergedRelatedCI>().ToLookup(x => "");
            var relationsOtherCIs = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(relationsOtherCIIDs), layers, true, trans, atTime)).ToDictionary(ci => ci.ID);
            var relationsAndToCIs = relations.Select(r => MergedRelatedCI.Build(r.Relation, ciid, relationsOtherCIs[(r.Relation.FromCIID == ciid) ? r.Relation.ToCIID : r.Relation.FromCIID]));
            return relationsAndToCIs.ToLookup(r => r.PredicateID);
        }

        public static async Task<IEnumerable<CompactRelatedCI>> GetCompactRelatedCIs(Guid ciid, LayerSet layerset, ICIModel ciModel, IRelationModel relationModel, int? perPredicateLimit, NpgsqlTransaction trans, TimeThreshold atTime)
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
                var relatedCompactCIs = (await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(relationTuples.Select(t => t.relatedCIID).Distinct()), layerset, trans, atTime))
                    .ToDictionary(ci => ci.ID); // TODO: performance improvements
                foreach ((var relation, var relatedCIID, var isForwardRelation) in relationTuples)
                {
                    var predicateID = relation.Relation.PredicateID;
                    var predicateWording = (isForwardRelation) ? relation.Relation.Predicate.WordingFrom : relation.Relation.Predicate.WordingTo;
                    var changesetID = relation.Relation.ChangesetID;
                    relatedCompactCIs.TryGetValue(relatedCIID, out var ci); // TODO: performance improvements
                    relatedCIs.Add(CompactRelatedCI.Build(ci, relation.Relation.ID, relation.Relation.FromCIID, relation.Relation.ToCIID, changesetID, predicateID, isForwardRelation, predicateWording, relation.LayerStackIDs));
                }
            }

            return relatedCIs;
        }
    }
}
