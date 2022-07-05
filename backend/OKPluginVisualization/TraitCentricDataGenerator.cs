using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OKPluginVisualization
{
    public class TraitCentricDataGenerator
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IBaseRelationModel baseRelationModel;
        private readonly ILayerDataModel layerDataModel;

        public TraitCentricDataGenerator(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel,
            IAttributeModel attributeModel, IRelationModel relationModel, IBaseRelationModel baseRelationModel, ILayerDataModel layerDataModel)
        {
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.baseRelationModel = baseRelationModel;
            this.layerDataModel = layerDataModel;
        }
        public async Task<string> GenerateDot(LayerSet layerSet, IEnumerable<ITrait> traits, IModelContext trans, TimeThreshold timeThreshold)
        {
            var (ciidsByTraitSet, layerIDsByCIID, relationEdges) = await Process(layerSet, traits, trans, timeThreshold);

            var layerData = await layerDataModel.GetLayerData(trans, timeThreshold);

            return RenderDot(ciidsByTraitSet, layerIDsByCIID, relationEdges, layerData);
        }

        public async Task<JsonDocument> GenerateCytoscape(LayerSet layerSet, IEnumerable<ITrait> traits, IModelContext trans, TimeThreshold timeThreshold)
        {
            var (ciidsByTraitSet, layerIDsByCIID, relationEdges) = await Process(layerSet, traits, trans, timeThreshold);

            var layerData = await layerDataModel.GetLayerData(trans, timeThreshold);

            return RenderCytoscape(ciidsByTraitSet, layerIDsByCIID, relationEdges, layerData);
        }

        private async Task<(IDictionary<string, IList<Guid>> ciidsByTraitSet, IDictionary<Guid, ISet<string>> layerIDsByCIID, IDictionary<(string from, string to, string predicateID), IList<MergedRelation>> relationEdges)> 
            Process(LayerSet layerSet, IEnumerable<ITrait> traits, IModelContext trans, TimeThreshold timeThreshold)
        {
            var predicateIDs = await baseRelationModel.GetPredicateIDs(RelationSelectionAll.Instance, layerSet.LayerIDs, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

            var traitIDsByCIID = new Dictionary<Guid, ISet<string>>();
            var layerIDsByCIID = new Dictionary<Guid, ISet<string>>();
            foreach (var trait in traits)
            {
                var traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel);
                var tes = await traitEntityModel.GetByCIID(AllCIIDsSelection.Instance, layerSet, trans, timeThreshold);
                foreach (var te in tes)
                {
                    var ciid = te.Key;
                    var traitID = te.Value.UnderlyingTrait.ID;
                    var affectingLayerIDs = te.Value.ExtractAffectingLayerIDs();
                    traitIDsByCIID.AddOrUpdate(ciid,
                        () => new HashSet<string>() { traitID },
                        (current) => { current.Add(traitID); return current; });
                    layerIDsByCIID.AddOrUpdate(ciid,
                        () => affectingLayerIDs,
                        (current) => { current.UnionWith(affectingLayerIDs); return current; });
                }
            }

            var ciidsByTraitSet = new Dictionary<string, IList<Guid>>();
            foreach (var kv in traitIDsByCIID)
            {
                var traitIDsForCI = kv.Value;
                var etsKey = string.Join('#', traitIDsForCI);
                if (ciidsByTraitSet.TryGetValue(etsKey, out var ciids))
                {
                    ciids.Add(kv.Key);
                }
                else
                {
                    ciidsByTraitSet[etsKey] = new List<Guid>() { kv.Key };
                }
            }

            var traitSetsByCIID = new Dictionary<Guid, string>();
            foreach (var kv in ciidsByTraitSet)
            {
                foreach (var ciid in kv.Value)
                    traitSetsByCIID[ciid] = kv.Key;
            }

            //var nodesWithEdges = new HashSet<string>();

            var relations = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(predicateIDs), layerSet, trans, timeThreshold, MaskHandlingForRetrievalApplyMasks.Instance, GeneratedDataHandlingInclude.Instance);

            var relationEdges = new Dictionary<(string from, string to, string predicateID), IList<MergedRelation>>();
            foreach (var relation in relations)
            {
                traitSetsByCIID.TryGetValue(relation.Relation.FromCIID, out string? fromTraitSetKey);
                traitSetsByCIID.TryGetValue(relation.Relation.ToCIID, out string? toTraitSetKey);

                if (fromTraitSetKey != null && toTraitSetKey != null)
                {
                    var key = (fromTraitSetKey, toTraitSetKey, relation.Relation.PredicateID);
                    if (relationEdges.TryGetValue(key, out var re))
                    {
                        re.Add(relation);
                    }
                    else
                    {
                        relationEdges[key] = new List<MergedRelation>() { relation };
                    }

                    //nodesWithEdges.Add(fromTraitSetKey);
                    //nodesWithEdges.Add(toTraitSetKey);
                }
            }

            return (ciidsByTraitSet, layerIDsByCIID, relationEdges);
        }

        private string RenderDot(IDictionary<string, IList<Guid>> ciidsByTraitSet, IDictionary<Guid, ISet<string>> layerIDsByCIID, 
            IDictionary<(string from, string to, string predicateID), IList<MergedRelation>> relationEdges, IDictionary<string, LayerData> layerData)
        {
            var sb = new StringBuilder();

            sb.AppendLine("digraph {");
            foreach (var kv in ciidsByTraitSet)
            {
                var traitSetKey = kv.Key;
                var ciids = kv.Value;
                //if (nodesWithEdges.Contains(traitSetKey))
                //{
                var traitIDsForNode = traitSetKey.Split('#');
                var affectingLayerIDs = ciids.SelectMany(ciid => layerIDsByCIID[ciid]).ToHashSet();
                var layerColors = affectingLayerIDs.Select(layerID => layerData.GetOrWithClass(layerID, null)?.Color).Where(color => color.HasValue).Select(color => ARGBToHexString(color!.Value));
                var layerColorIcons = string.Join("", layerColors.Select(color => $"<FONT COLOR=\"{color}\">&#9646;</FONT>"));
                var nodeColor = (layerColors.Count() == 1) ? $"\"{layerColors.First()}\"" : "black";

                sb.AppendLine($"\"{traitSetKey}\" [shape=box, color={nodeColor}, label=<{string.Join("<BR />", traitIDsForNode)}<BR />{layerColorIcons} ({kv.Value.Count()})>]");
                //}
            }
            foreach (var relationEdge in relationEdges)
            {
                var key = relationEdge.Key;
                var affectingLayerIDs = relationEdge.Value.Select(r => r.LayerStackIDs.First()).ToHashSet();
                var layerColors = affectingLayerIDs.Select(layerID => layerData.GetOrWithClass(layerID, null)?.Color).Where(color => color.HasValue).Select(color => ARGBToHexString(color!.Value));
                var layerColorIcons = string.Join("", layerColors.Select(color => $"<FONT COLOR=\"{color}\">&#9646;</FONT>"));
                var edgeColor = (layerColors.Count() == 1) ? $"\"{layerColors.First()}\"" : "black";

                sb.AppendLine($"\"{key.from}\" -> \"{key.to}\" [color={edgeColor}, label=<{layerColorIcons} {key.predicateID}<BR />({relationEdge.Value.Count()})>]");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }

        private JsonDocument RenderCytoscape(IDictionary<string, IList<Guid>> ciidsByTraitSet, IDictionary<Guid, ISet<string>> layerIDsByCIID,
            IDictionary<(string from, string to, string predicateID), IList<MergedRelation>> relationEdges, IDictionary<string, LayerData> layerData)
        {
            var elements = new JsonArray();
            foreach (var kv in ciidsByTraitSet)
            {
                var traitSetKey = kv.Key;
                var ciids = kv.Value;
                var traitIDsForNode = traitSetKey.Split('#');
                var affectingLayerIDs = ciids.SelectMany(ciid => layerIDsByCIID[ciid]).ToHashSet();
                var layerColors = affectingLayerIDs.Select(layerID => layerData.GetOrWithClass(layerID, null)?.Color).Where(color => color.HasValue).Select(color => ARGBToHexString(color!.Value));
                //var layerColorIcons = string.Join("", layerColors.Select(color => $"<FONT COLOR=\"{color}\">&#9646;</FONT>"));
                var nodeColor = (layerColors.Count() == 1) ? layerColors.First() : "black";

                var label = $"{string.Join("\n", traitIDsForNode)}\n({kv.Value.Count()})";
                elements.Add(new JsonObject()
                {
                    ["data"] = new JsonObject() { 
                        ["id"] = traitSetKey,
                        ["label"] = label,
                        //["color"] = nodeColor,
                        ["colors"] = JsonSerializer.SerializeToNode(layerColors.ToArray())
                    },
                });
            }
            foreach (var relationEdge in relationEdges)
            {
                var key = relationEdge.Key;
                var affectingLayerIDs = relationEdge.Value.Select(r => r.LayerStackIDs.First()).ToHashSet();
                var layerColors = affectingLayerIDs.Select(layerID => layerData.GetOrWithClass(layerID, null)?.Color).Where(color => color.HasValue).Select(color => ARGBToHexString(color!.Value));
                //var layerColorIcons = string.Join("", layerColors.Select(color => $"<FONT COLOR=\"{color}\">&#9646;</FONT>"));
                var edgeColor = (layerColors.Count() == 1) ? layerColors.First() : "black";

                elements.Add(new JsonObject()
                {
                    ["data"] = new JsonObject()
                    {
                        ["id"] = $"{key.from} -> {key.predicateID} -> {key.to}",
                        ["source"] = key.from,
                        ["target"] = key.to,
                        ["label"] = $"{ key.predicateID }\n({ relationEdge.Value.Count() })",
                        //["color"] = edgeColor,
                        ["colors"] = JsonSerializer.SerializeToNode(layerColors.ToArray())
                    }
                });
            }

            return JsonSerializer.SerializeToDocument(elements);
        }

        private string ARGBToHexString(long color)
        {
            return '#' + (color & 0xFFFFFF).ToString("X6");
        }
    }
}
