using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OKPluginVisualization
{
    public class LayerCentricUsageGenerator
    {
        private readonly IUsageStatsModel usageStatsModel;
        private readonly ILayerDataModel layerDataModel;

        public LayerCentricUsageGenerator(IUsageStatsModel usageStatsModel, ILayerDataModel layerDataModel)
        {
            this.usageStatsModel = usageStatsModel;
            this.layerDataModel = layerDataModel;
        }
        public async Task<string> Generate(LayerSet layerSet, DateTimeOffset timeFrom, DateTimeOffset timeTo, IModelContext trans, TimeThreshold timeThreshold)
        {
            var elements = await usageStatsModel.GetElements(timeFrom, timeTo, trans);

            var attributeAccesses = elements
                .Where(e => e.Type == "attribute")
                .Where(e => e.Operation == UsageStatsOperation.Read || e.Operation == UsageStatsOperation.Write)
                .Where(e => layerSet.Contains(e.LayerID))
                .DistinctBy(e => (e.LayerID, e.Name, e.Username))
                .ToList();

            var relationAccesses = elements
                .Where(e => e.Type == "relation-predicate")
                .Where(e => e.Operation == UsageStatsOperation.Read || e.Operation == UsageStatsOperation.Write)
                .Where(e => layerSet.Contains(e.LayerID))
                .DistinctBy(e => (e.LayerID, e.Name, e.Username))
                .ToList();

            var users = attributeAccesses.Select(e => e.Username).Union(relationAccesses.Select(e => e.Username)).Distinct().ToList();

            var layerData = await layerDataModel.GetLayerData(trans, timeThreshold);

            var sb = new StringBuilder();

            sb.AppendLine("digraph {");
            sb.AppendLine("rankdir = LR;");
            sb.AppendLine("node[shape = record];");
            //sb.AppendLine("splines = ortho;");

            string buildAttributeNodeName(string attribute, string layerID)
            {
                return $"attribute_{attribute.Replace("*", "STAR").Replace(".", "DOT")}_AT_{layerID}";
            }
            string buildRelationNodeName(string attribute, string layerID)
            {
                return $"relation_{attribute.Replace("*", "STAR").Replace(".", "DOT")}_AT_{layerID}";
            }
            string buildUserNodeName(string username)
            {
                return $"user_{username.Replace("*", "STAR").Replace(".", "DOT").Replace("-", "MINUS").Replace("@", "AT")}";
            }
            string buildInnerAttributeTableNodeName(string layerID)
            {
                return $"attributes_{layerID}";
            }
            string buildInnerRelationTableNodeName(string layerID)
            {
                return $"relations_{layerID}";
            }

            // users
            var clbUsernameRegexPattern = "__cl\\.(.*)\\@.*";
            foreach (var username in users)
            {
                var extractedUsername = username;
                var isCLB = false;
                var matches = Regex.Matches(username, clbUsernameRegexPattern);
                if (matches.Count() > 0)
                {
                    isCLB = true;
                    extractedUsername = matches.First().Groups[1].Value;
                }
                var label = isCLB ? $"CLB:\\n{extractedUsername}" : $"User:\\n{extractedUsername}";
                sb.AppendLine($"{buildUserNodeName(username)} [label = \"{label}\"];");
            }

            // edges
            // NOTE: we create the edges a bit complicated and play with backward/forward directed edges,
            // because we want to force CLBs to be on the left and regular users to be on the right-side of layers
            foreach (var @as in attributeAccesses)
            {
                var label = "\"\"";
                var style = @as.Operation == UsageStatsOperation.Read ? "dashed" : "solid";
                if (Regex.IsMatch(@as.Username, clbUsernameRegexPattern))
                {
                    var dir = @as.Operation == UsageStatsOperation.Read ? "back" : "forward";
                    sb.AppendLine($"{buildUserNodeName(@as.Username)} -> {buildInnerAttributeTableNodeName(@as.LayerID)}:{buildAttributeNodeName(@as.Name, @as.LayerID)} [label={label}, dir={dir}, style={style}]");
                }
                else
                {
                    var dir = @as.Operation == UsageStatsOperation.Write ? "back" : "forward";
                    sb.AppendLine($"{buildInnerAttributeTableNodeName(@as.LayerID)}:{buildAttributeNodeName(@as.Name, @as.LayerID)} -> {buildUserNodeName(@as.Username)} [label={label}, dir={dir}, style={style}]");
                }
            }
            foreach (var rs in relationAccesses)
            {
                var label = "\"\"";
                var style = rs.Operation == UsageStatsOperation.Read ? "dashed" : "solid";
                if (Regex.IsMatch(rs.Username, clbUsernameRegexPattern))
                {
                    var dir = rs.Operation == UsageStatsOperation.Read ? "back" : "forward";
                    sb.AppendLine($"{buildUserNodeName(rs.Username)} -> {buildInnerRelationTableNodeName(rs.LayerID)}:{buildRelationNodeName(rs.Name, rs.LayerID)} [label={label}, dir={dir}, style={style}]");
                }
                else
                {
                    var dir = rs.Operation == UsageStatsOperation.Write ? "back" : "forward";
                    sb.AppendLine($"{buildInnerRelationTableNodeName(rs.LayerID)}:{buildRelationNodeName(rs.Name, rs.LayerID)} -> {buildUserNodeName(rs.Username)} [label={label}, dir={dir}, style={style}]");
                }
            }


            // layers
            var attributesPerLayer = attributeAccesses.GroupBy(e => e.LayerID, e => e.Name).ToDictionary(e => e.Key, e => e.Distinct().ToList());
            var relationPerLayer = relationAccesses.GroupBy(e => e.LayerID, e => e.Name).ToDictionary(e => e.Key, e => e.Distinct().ToList());
            foreach (var layerID in layerSet)
            {
                sb.AppendLine($"subgraph cluster_layer_{layerID} {{");
                sb.AppendLine($"label = \"Layer {layerID}\"");
                if (layerData.TryGetValue(layerID, out var ld))
                {
                    sb.AppendLine($"color = \"{ARGBToHexString(ld.Color)}\"");
                }
                sb.AppendLine($"penwidth = 5");

                // attributes
                if (attributesPerLayer.TryGetValue(layerID, out var attributesOfLayer))
                {
                    sb.AppendLine($"subgraph cluster_layer_attributes_{layerID} {{");
                    sb.AppendLine($"label = \"Attributes\"");
                    sb.AppendLine($"penwidth = 0");
                    sb.AppendLine($"{buildInnerAttributeTableNodeName(layerID)}[shape = none, label =<");
                    sb.AppendLine("<table border = \"0\" cellborder = \"1\" cellspacing = \"0\">");
                    foreach (var attribute in attributesOfLayer)
                        sb.AppendLine($"<tr><td port=\"{buildAttributeNodeName(attribute, layerID)}\" align=\"LEFT\" balign=\"LEFT\" >{attribute}<br align=\"left\"/></td></tr>");
                    sb.AppendLine("</table>");
                    sb.AppendLine(">]");
                    sb.AppendLine("}");
                }

                // relations
                if (relationPerLayer.TryGetValue(layerID, out var relationsOfLayer))
                {
                    sb.AppendLine($"subgraph cluster_layer_relations_{layerID} {{");
                    sb.AppendLine($"label = \"Relations\"");
                    sb.AppendLine($"penwidth = 0");
                    sb.AppendLine($"{buildInnerRelationTableNodeName(layerID)}[shape = none, label =<");
                    sb.AppendLine("<table border = \"0\" cellborder = \"1\" cellspacing = \"0\">");
                    foreach (var relation in relationsOfLayer)
                        sb.AppendLine($"<tr><td port=\"{buildRelationNodeName(relation, layerID)}\" align=\"LEFT\" balign=\"LEFT\" >{relation}<br align=\"left\"/></td></tr>");
                    sb.AppendLine("</table>");
                    sb.AppendLine(">]");
                    sb.AppendLine("}");
                }

                sb.AppendLine("}");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }
        private string ARGBToHexString(long color)
        {
            return '#' + (color & 0xFFFFFF).ToString("X6");
        }
    }
}
