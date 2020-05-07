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
        public static async Task<IEnumerable<(Relation relation, MergedCI toCI)>> GetMergedForwardRelationsAndToCIs(
            Guid ciid, LayerSet layers, ICIModel ciModel, IRelationModel relationModel, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var relations = await relationModel.GetMergedRelations(ciid, false, layers, IncludeRelationDirections.Forward, trans, atTime);
            var relationsToCIIDs = relations.Select(r => r.ToCIID).Distinct();
            var relationsToCIs = (await ciModel.GetMergedCIs(layers, true, trans, atTime, relationsToCIIDs)).ToDictionary(ci => ci.ID);
            var relationsAndToCIs = relations.Select(r => (relation: r, toCI: relationsToCIs[r.ToCIID]));
            return relationsAndToCIs;
        }

        public static async Task<IEnumerable<CompactRelatedCI>> GetCompactRelatedCIs(Guid ciid, LayerSet layerset, ICIModel ciModel, IRelationModel relationModel, int? perPredicateLimit, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var relations = await relationModel.GetMergedRelations(ciid, false, layerset, IncludeRelationDirections.Both, trans, atTime);

            // HACK: limit number per predicate type
            var predicateCounts = new Dictionary<string, int>();
            var limitedRelations = new List<Relation>();
            if (perPredicateLimit.HasValue)
            {
                foreach (var r in relations)
                {
                    var current = 0;
                    predicateCounts.TryGetValue(r.PredicateID, out current);
                    predicateCounts[r.PredicateID] = current + 1;
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
                var isForwardRelation = r.FromCIID == ciid;
                var relatedCIID = (isForwardRelation) ? r.ToCIID : r.FromCIID;
                return (relation: r, relatedCIID, isForwardRelation);
            });

            var relatedCIs = new List<CompactRelatedCI>();
            var relatedCompactCIs = (await ciModel.GetCompactCIs(layerset, trans, atTime, relationTuples.Select(t => t.relatedCIID).Distinct()))
            .ToDictionary(ci => ci.ID); // TODO: performance improvements
            foreach ((var relation, var relatedCIID, var isForwardRelation) in relationTuples)
            {
                var predicateID = relation.PredicateID;
                var predicateWording = (isForwardRelation) ? relation.Predicate.WordingFrom : relation.Predicate.WordingTo;
                var changesetID = relation.ChangesetID;
                relatedCompactCIs.TryGetValue(relatedCIID, out var ci); // TODO: performance improvements
                relatedCIs.Add(CompactRelatedCI.Build(ci, relation.FromCIID, relation.ToCIID, changesetID, predicateID, isForwardRelation, predicateWording, relation.LayerStackIDs));
            }

            return relatedCIs;
        }
    }
}
