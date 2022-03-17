using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKPluginVisualization
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class GraphvizDotController : ControllerBase
    {
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ITraitsProvider traitsProvider;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IBaseRelationModel baseRelationModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly ICurrentUserService currentUserService;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public GraphvizDotController(IModelContextBuilder modelContextBuilder, ITraitsProvider traitsProvider, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, 
            IAttributeModel attributeModel, IRelationModel relationModel, IBaseRelationModel baseRelationModel, ILayerDataModel layerDataModel, ICurrentUserService currentUserService,
            ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.modelContextBuilder = modelContextBuilder;
            this.traitsProvider = traitsProvider;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.baseRelationModel = baseRelationModel;
            this.layerDataModel = layerDataModel;
            this.currentUserService = currentUserService;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        private string ARGBToHexString(long color)
        {
            return '#' + (color & 0xFFFFFF).ToString("X6");
        }

        [HttpGet("generate")]
        public async Task<IActionResult> Generate([FromQuery, Required] string[] layerIDs, [FromQuery] string[] traitIDs)
        {
            if (layerIDs.IsEmpty())
                return BadRequest("No layer IDs specified");

            using var trans = modelContextBuilder.BuildImmediate();
            var user = await currentUserService.GetCurrentUser(trans);
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, layerIDs))
                return Forbid($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerIDs)}");

            var layerSet = new LayerSet(layerIDs);

            var timeThreshold = TimeThreshold.BuildLatest();

            //var allTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);
            IEnumerable<ITrait> traits;
            if (traitIDs.IsEmpty())
                traits = (await traitsProvider.GetActiveTraits(trans, timeThreshold)).Values;
            else
                traits = (await traitsProvider.GetActiveTraitsByIDs(traitIDs, trans, timeThreshold)).Values;
            //var traits = allTraits.Values.Where(t => t.ID != "named");//.Where(t => Regex.Match(t.ID, @"tsa_cmdb\..*").Success || Regex.Match(t.ID, @"monman_v2\..*").Success); // TODO

            var predicateIDs = await baseRelationModel.GetPredicateIDs(RelationSelectionAll.Instance, layerSet.LayerIDs, trans, timeThreshold, GeneratedDataHandlingInclude.Instance);

            var traitIDsByCIID = new Dictionary<Guid, ISet<string>>();
            var layerIDsByCIID = new Dictionary<Guid, ISet<string>>();
            foreach (var trait in traits)
            {
                var traitEntityModel = new TraitEntityModel(trait, effectiveTraitModel, ciModel, attributeModel, relationModel);
                var tes = await traitEntityModel.GetByCIID(new AllCIIDsSelection(), layerSet, trans, timeThreshold);
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
            foreach(var kv in traitIDsByCIID)
            {
                var traitIDsForCI = kv.Value;
                var etsKey = string.Join('#', traitIDsForCI);
                if (ciidsByTraitSet.TryGetValue(etsKey, out var ciids))
                {
                    ciids.Add(kv.Key);
                } else
                {
                    ciidsByTraitSet[etsKey] = new List<Guid>() { kv.Key };
                }
            }

            var traitSetsByCIID = new Dictionary<Guid, string>();
            foreach(var kv in ciidsByTraitSet)
            {
                foreach(var ciid in kv.Value)
                    traitSetsByCIID[ciid] = kv.Key;
            }

            var nodesWithEdges = new HashSet<string>();

            // TODO: implement multiple predicates selection
            var relations = new List<MergedRelation>();
            foreach (var predicateID in predicateIDs)
            {
                var r = await relationModel.GetMergedRelations(RelationSelectionWithPredicate.Build(predicateID), layerSet, trans, timeThreshold, MaskHandlingForRetrievalApplyMasks.Instance, GeneratedDataHandlingInclude.Instance);
                relations.AddRange(r);
            }

            var relationEdges = new Dictionary<(string from, string to, string predicateID), IList<MergedRelation>>();
            foreach(var relation in relations)
            {
                string? fromTraitSetKey = null;
                traitSetsByCIID.TryGetValue(relation.Relation.FromCIID, out fromTraitSetKey);
                string? toTraitSetKey = null;
                traitSetsByCIID.TryGetValue(relation.Relation.ToCIID, out toTraitSetKey);

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

                    nodesWithEdges.Add(fromTraitSetKey);
                    nodesWithEdges.Add(toTraitSetKey);
                }
            }

            var layerData = await layerDataModel.GetLayerData(trans, timeThreshold);

            var sb = new StringBuilder();

            sb.AppendLine("digraph {");
            foreach(var kv in ciidsByTraitSet)
            {
                var traitSetKey = kv.Key;
                var ciids = kv.Value;

                var traitIDsForNode = traitSetKey.Split('#');
                var affectingLayerIDs = ciids.SelectMany(ciid => layerIDsByCIID[ciid]).ToHashSet();

                var layerColors = affectingLayerIDs.Select(layerID => layerData.GetOrWithClass(layerID, null)?.Color).Where(color => color.HasValue).Select(color => ARGBToHexString(color!.Value));

                var layerColorIcons = string.Join("", layerColors.Select(color => $"<FONT COLOR=\"{color}\">&#9646;</FONT>"));

                if (nodesWithEdges.Contains(traitSetKey))
                    sb.AppendLine($"\"{traitSetKey}\" [label=<{layerColorIcons} {string.Join(" &amp;&amp; ", traitIDsForNode)}<BR />({kv.Value.Count()})>]");
            }
            foreach(var relationEdge in relationEdges)
            {
                var key = relationEdge.Key;
                sb.AppendLine($"\"{key.from}\" -> \"{key.to}\" [label=<{key.predicateID}<BR />({relationEdge.Value.Count()})>]");
            }
            sb.AppendLine("}");

            return Content(sb.ToString());
        }
    }
}
