using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Helper
{
    public class AttributeResolver
    {
        private ILookup<(string PredicateID, Guid FromCIID), Guid> outgoingRelationsLookup = null!;
        private ILookup<(string PredicateID, Guid ToCIID), Guid> incomingRelationsLookup = null!;
        private IDictionary<Guid, MergedCI> relatedMergedCIsLookup = new Dictionary<Guid, MergedCI>();

        public async Task PrefetchRelatedCIsAndLookups(GridViewConfiguration config, ISet<Guid> baseCIIDs, IRelationModel relationModel, ICIModel ciModel, IModelContext trans, TimeThreshold atTime)
        {
            // collected necessary outgoing and incoming relations via the specified predicates in the SourceAttributePaths
            // TODO: validate sourceAttributePaths:
            // must be null or length=2
            // if length=2, first must be "<" or ">", second must be predicateID
            var pathsRequiringRelations = config.Columns.Where(c => c.SourceAttributePath != null).Select(c => (attributeName: c.SourceAttributeName, path: c.SourceAttributePath!)).Where(t => t.path.Length == 2).Select(t => (direction: t.path[0], predicateID: t.path[1], attributeName: t.attributeName));
            var outgoingPathsRequiringRelations = pathsRequiringRelations.Where(path => path.direction == ">").ToLookup(path => path.predicateID);
            var incomingPathsRequiringRelations = pathsRequiringRelations.Where(path => path.direction == "<").ToLookup(path => path.predicateID);

            // TODO: improve Relation Selection fetching, fetch only those that fit both from/to AND predicate
            ISet<Guid> outgoingRelatedCIIDs = new HashSet<Guid>();
            if (outgoingPathsRequiringRelations.Count() > 0)
            {
                // TODO: masking
                var outgoingRelations = (await relationModel.GetMergedRelations(RelationSelectionFrom.Build(baseCIIDs), new LayerSet(config.ReadLayerset), trans, atTime, MaskHandlingForRetrievalGetMasks.Instance))
                    .Where(r => outgoingPathsRequiringRelations.Contains(r.Relation.PredicateID));
                outgoingRelationsLookup = (outgoingRelations).ToLookup(r => (r.Relation.PredicateID, r.Relation.FromCIID), r => r.Relation.ToCIID);
                outgoingRelatedCIIDs = outgoingRelations.Select(r => r.Relation.ToCIID).ToHashSet();
            }
            ISet<Guid> incomingRelatedCIIDs = new HashSet<Guid>();
            if (incomingPathsRequiringRelations.Count() > 0)
            {
                // TODO: masking
                var incomingRelations = (await relationModel.GetMergedRelations(RelationSelectionTo.Build(baseCIIDs), new LayerSet(config.ReadLayerset), trans, atTime, MaskHandlingForRetrievalGetMasks.Instance))
                       .Where(r => incomingPathsRequiringRelations.Contains(r.Relation.PredicateID));
                incomingRelationsLookup = (incomingRelations).ToLookup(r => (r.Relation.PredicateID, r.Relation.ToCIID), r => r.Relation.FromCIID);
                incomingRelatedCIIDs = incomingRelations.Select(r => r.Relation.FromCIID).ToHashSet();
            }

            var relatedCIIDs = outgoingRelatedCIIDs.Union(incomingRelatedCIIDs).ToHashSet();

            // limit the fetched attributes to only those that are relevant for the gridview
            // NOTE: we do this across relations, so when there are different relation fetches, this selection might fetch more than necessary, because we only use a single list of attributes
            // could be an area of performance improvement
            var relatedCIAttributes = pathsRequiringRelations.Select(t => t.attributeName).ToHashSet();

            relatedMergedCIsLookup = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(relatedCIIDs), new LayerSet(config.ReadLayerset), false, NamedAttributesSelection.Build(relatedCIAttributes), trans, atTime)).ToDictionary(ci => ci.ID);
        }

        private bool TryGetAttributeFromAttributeName(MergedCI baseCI, string attributeName, [MaybeNullWhen(false)] out MergedCIAttribute attribute)
        {
            return baseCI.MergedAttributes.TryGetValue(attributeName, out attribute);
        }

        public bool TryResolveAttribute(MergedCI baseCI, GridViewColumn column, [MaybeNullWhen(false)] out MergedCIAttribute attribute)
        {
            var ci = baseCI;
            if (column.SourceAttributePath != null)
            {
                var sap = column.SourceAttributePath;
                if (sap.Length == 2)
                {
                    var isOutgoing = sap[0] == ">";
                    var predicateID = sap[1];

                    var relationsLookup = (isOutgoing) ? outgoingRelationsLookup : incomingRelationsLookup;

                    var relatedCIIDs = relationsLookup[(predicateID, baseCI.ID)];
                    if (relatedCIIDs.IsEmpty())
                    {
                        attribute = null;
                        return false;
                    }
                    else
                    {
                        var relatedCIID = relatedCIIDs.OrderBy(ciid => ciid).First(); // we order by GUID to stay consistent even when multiple CIs would match
                        if (relatedMergedCIsLookup.TryGetValue(relatedCIID, out var relatedCI))
                        {
                            ci = relatedCI;
                        }
                        else
                        {
                            attribute = null;
                            return false;
                        }
                    }
                }
                else
                    throw new Exception($"Invalid source attribute path \"{column.SourceAttributePath}\" detected");
            }

            return TryGetAttributeFromAttributeName(ci, column.SourceAttributeName, out attribute);
        }
    }
}
