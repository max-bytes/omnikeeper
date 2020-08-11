using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Service;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Newtonsoft.Json.Linq;
using Npgsql;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Landscape.Base.Templating
{
    public class VariableNode : ScriptObject
    {
        private readonly string scalar;

        public VariableNode(string scalar) : base()
        {
            this.scalar = scalar;
        }
        public override string ToString(TemplateContext context, SourceSpan span) => scalar;
    }

    public static class ScribanVariableService
    {
        private static object AttributeValue2VariableValue(IAttributeValue value)
        {
            var v = value.ToGenericObject();

            return v;
        }
        // transform dots in variable name into corresponding object structure
        private static void AddNested(IDictionary<string, object> dict, string key, IAttributeValue value)
        {
            var split = key.Split(".", 2, StringSplitOptions.None);
            if (split.Length == 1)
            {
                dict.Add(key, AttributeValue2VariableValue(value));
            }
            else
            {
                var keyPre = split[0];
                var keySuffix = split[1];
                if (!dict.TryGetValue(keyPre, out var subO))
                {
                    var sub = new Dictionary<string, object>();
                    dict.Add(keyPre, sub);
                    AddNested(sub, keySuffix, value);
                }
                else
                {
                    if (subO is VariableNode vn)
                    { // special case: sub is already variable node -> treat as regular dict
                        AddNested(vn, keySuffix, value);
                    }
                    else if (subO is IDictionary<string, object> sub)
                    {
                        AddNested(sub, keySuffix, value);
                    }
                    else
                    { // sub is some kind of value -> replace with custom variableNode
                        if (subO is string subStr)
                        {
                            var vnNew = new VariableNode(subStr);
                            dict[keyPre] = vnNew;
                            AddNested(vnNew, keySuffix, value);
                        }
                        else
                        {
                            // TODO: how to handle this case? already present value is not a string (an array perhaps?)
                            var vnNew = new VariableNode(subO.ToString()); // HACK: transform to string, for now
                            dict[keyPre] = vnNew;
                            AddNested(vnNew, keySuffix, value);
                        }
                    }
                }
            }
        }

        public class ScriptObjectContext
        {
            public ScriptObjectContext(LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime, ICIModel ciModel, IRelationModel relationModel)
            {
                Layerset = layerset;
                Transaction = trans;
                AtTime = atTime;
                CIModel = ciModel;
                RelationModel = relationModel;
            }

            public LayerSet Layerset { get; }
            public NpgsqlTransaction Transaction { get; }
            public TimeThreshold AtTime { get; }
            public ICIModel CIModel { get; }
            public IRelationModel RelationModel { get; }
        }

        public class ScriptObjectRelatedCIs : ScriptObject
        {
            public ScriptObjectRelatedCIs(IEnumerable<CompactRelatedCI> relatedCIs, ScriptObjectContext context)
            {
                Add("forward", relatedCIs.Where(r => r.IsForwardRelation).GroupBy(r => r.PredicateID)
                    .ToDictionary(t => t.Key, t => t.Select(r => {
                        var ci = context.CIModel.GetMergedCI(r.CI.ID, context.Layerset, context.Transaction, context.AtTime).GetAwaiter().GetResult();
                        return new ScriptObjectCI(ci, context);
                    })));
                Add("back", relatedCIs.Where(r => !r.IsForwardRelation).GroupBy(r => r.PredicateID)
                    .ToDictionary(t => t.Key, t => t.Select(r => {
                        var ci = context.CIModel.GetMergedCI(r.CI.ID, context.Layerset, context.Transaction, context.AtTime).GetAwaiter().GetResult();
                        return new ScriptObjectCI(ci, context);
                    })));
            }
        }
        
        public class ScriptObjectCI : ScriptObject
        {
            public ScriptObjectCI(MergedCI ci, ScriptObjectContext context)
            {
                Add("id", ci.ID);
                Add("name", ci.Name);
                this.Import("attributes", new Func<Dictionary<string, object>>(() =>
                {
                    // TODO: caching
                    var attributeVariables = new Dictionary<string, object>();
                    foreach (var monitoredCIAttribute in ci.MergedAttributes.Values)
                        AddNested(attributeVariables, $"{monitoredCIAttribute.Attribute.Name}", monitoredCIAttribute.Attribute.Value);
                    return attributeVariables;
                }));
                this.Import("relations", new Func<ScriptObjectRelatedCIs>(() => {
                    var relatedCIs = RelationService.GetCompactRelatedCIs(ci.ID, context.Layerset, context.CIModel, context.RelationModel, null, context.Transaction, context.AtTime)
                        .GetAwaiter().GetResult(); // HACK, TODO: async
                    return new ScriptObjectRelatedCIs(relatedCIs, context);
                }));
            }
        }

        public static TemplateContext CreateCIBasedTemplateContext(MergedCI ci, LayerSet layerSet, TimeThreshold atTime, NpgsqlTransaction trans, ICIModel ciModel, IRelationModel relationModel)
        {
            var so = new ScriptObjectCI(ci, new ScriptObjectContext(layerSet, trans, atTime, ciModel, relationModel));
            var context = new TemplateContext();
            //context.StrictVariables = true;
            context.EnableRelaxedMemberAccess = true;
            context.PushGlobal(new ScriptObject() { { "target", so } });
            return context;
        }
    }
}
