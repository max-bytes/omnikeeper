using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Templating
{
    public class VariableNode : ScriptObject
    {
        private readonly string scalar;

        public VariableNode(string scalar) : base()
        {
            this.scalar = scalar;
        }
        public override string ToString(string format, IFormatProvider formatProvider) => scalar;
    }

    public static class ScribanVariableService
    {
        private static object AttributeValue2VariableValue(IAttributeValue value)
        {
            var v = value.ToGenericObject();

            return v;
        }

        // transform dots in variable name into corresponding object structure
        //private static void AddSimple(IDictionary<string, object> dict, string key, IAttributeValue value)
        //{
        //    dict.Add(key, AttributeValue2VariableValue(value));
        //}

        // transform dots in variable name into corresponding object structure
        [Obsolete]
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
                            var vnNew = new VariableNode(subO.ToString()!); // HACK: transform to string, for now
                            dict[keyPre] = vnNew;
                            AddNested(vnNew, keySuffix, value);
                        }
                    }
                }
            }
        }

        [Obsolete]
        public class ScriptObjectComplexContext
        {
            public ScriptObjectComplexContext(LayerSet layerset, IModelContext trans, TimeThreshold atTime, ICIModel ciModel, IRelationModel relationModel)
            {
                Layerset = layerset;
                Transaction = trans;
                AtTime = atTime;
                CIModel = ciModel;
                RelationModel = relationModel;
            }

            public LayerSet Layerset { get; }
            public IModelContext Transaction { get; }
            public TimeThreshold AtTime { get; }
            public ICIModel CIModel { get; }
            public IRelationModel RelationModel { get; }
        }

        //public class ScriptObjectRelatedCIs : ScriptObject
        //{
        //    public ScriptObjectRelatedCIs(IEnumerable<CompactRelatedCI> relatedCIs, ScriptObjectComplexContext context)
        //    {
        //        Add("forward", relatedCIs.Where(r => r.IsForwardRelation).GroupBy(r => r.PredicateID)
        //            .ToDictionary(t => t.Key, t => t.Select(r =>
        //            {
        //                var ci = context.CIModel.GetMergedCI(r.CI.ID, context.Layerset, context.Transaction, context.AtTime).GetAwaiter().GetResult();
        //                return new ScriptObjectComplexCI(ci, context);
        //            })));
        //        Add("back", relatedCIs.Where(r => !r.IsForwardRelation).GroupBy(r => r.PredicateID)
        //            .ToDictionary(t => t.Key, t => t.Select(r =>
        //            {
        //                var ci = context.CIModel.GetMergedCI(r.CI.ID, context.Layerset, context.Transaction, context.AtTime).GetAwaiter().GetResult();
        //                return new ScriptObjectComplexCI(ci, context);
        //            })));
        //    }
        //}

        [Obsolete]
        public class ScriptObjectComplexCI : ScriptObject
        {
            public ScriptObjectComplexCI(MergedCI ci, ScriptObjectComplexContext context)
            {
                Add("id", ci.ID);
                Add("name", ci.CIName);
                this.Import("attributes", new Func<Dictionary<string, object>>(() =>
                {
                    // TODO: caching
                    var attributeVariables = new Dictionary<string, object>();
                    foreach (var monitoredCIAttribute in ci.MergedAttributes.Values)
                        AddNested(attributeVariables, $"{monitoredCIAttribute.Attribute.Name}", monitoredCIAttribute.Attribute.Value);
                    return attributeVariables;
                }));
                //this.Import("relations", new Func<ScriptObjectRelatedCIs>(() =>
                //{
                //    var relatedCIs = RelationService.GetCompactRelatedCIs(ci.ID, context.Layerset, context.CIModel, context.RelationModel, context.Transaction, context.AtTime)
                //        .GetAwaiter().GetResult(); // HACK, TODO: async
                //    return new ScriptObjectRelatedCIs(relatedCIs, context);
                //}));
            }
        }

        [Obsolete]
        public static TemplateContext CreateComplexCIBasedTemplateContext(MergedCI ci, LayerSet layerSet, TimeThreshold atTime, IModelContext trans, ICIModel ciModel, IRelationModel relationModel)
        {
            var so = new ScriptObjectComplexCI(ci, new ScriptObjectComplexContext(layerSet, trans, atTime, ciModel, relationModel));
            var context = new TemplateContext
            {
                //context.StrictVariables = true;
                EnableRelaxedMemberAccess = true
            };
            context.PushGlobal(new ScriptObject() { { "target", so } });
            return context;
        }

        [Obsolete]
        public class ScriptObjectSimpleCI : ScriptObject
        {
            public ScriptObjectSimpleCI(MergedCI ci)
            {
                Add("id", ci.ID);
                Add("name", ci.CIName);
                this.Import("a", new Func<Dictionary<string, object>>(() =>
                {
                    // TODO: caching
                    var attributeVariables = new Dictionary<string, object>();
                    foreach (var attributeValues in ci.MergedAttributes.Values)
                        AddNested(attributeVariables, attributeValues.Attribute.Name, attributeValues.Attribute.Value);
                    return attributeVariables;
                }));
            }
        }
        [Obsolete]
        public static TemplateContext CreateSimpleCIBasedTemplateContext(MergedCI ci)
        {
            var so = new ScriptObjectSimpleCI(ci);
            var context = new TemplateContext
            {
                //context.StrictVariables = true;
                EnableRelaxedMemberAccess = true
            };
            context.PushGlobal(so);
            return context;
        }

        public class ScriptObjectAttributes : ScriptObject
        {
            public ScriptObjectAttributes(IEnumerable<CIAttribute> attributes)
            {
                this.Import("attributes", new Func<Dictionary<string, object>>(() =>
                {
                    // TODO: caching
                    return attributes.ToDictionary(a => a.Name, a => a.Value.ToGenericObject());
                }));
            }
        }
        public static TemplateContext CreateAttributesBasedTemplateContext(IEnumerable<CIAttribute> attributes)
        {
            var so = new ScriptObjectAttributes(attributes);
            var context = new TemplateContext
            {
                //context.StrictVariables = true;
                EnableRelaxedMemberAccess = true
            };
            context.PushGlobal(so);
            return context;
        }
    }
}
