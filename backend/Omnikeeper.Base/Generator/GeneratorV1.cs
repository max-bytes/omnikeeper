using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Templating;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Scriban;
using Scriban.Parsing;
using Scriban.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Generator
{
    public class GeneratorV1
    {
        public GeneratorV1(string id, string attributeName, GeneratorAttributeValue value)
        {
            ID = id;
            AttributeName = attributeName;
            Value = value;
        }

        public string ID { get; }
        public string AttributeName { get; }
        public GeneratorAttributeValue Value { get; }
    }

    public class GeneratorAttributeValue
    {
        private GeneratorAttributeValue(Template template, string templateStr, ISet<string> usedAttributeNames)
        {
            Template = template;
            TemplateStr = templateStr;
            UsedAttributeNames = usedAttributeNames;
        }

        public Template Template { get; }
        public string TemplateStr { get; }
        public ISet<string> UsedAttributeNames { get; }

        private class AttributeNameScriptVisitor : ScriptVisitor
        {
            public readonly ISet<string> AttributeNames = new HashSet<string>();

            public override void Visit(ScriptMemberExpression node)
            {
                if (node.Target is ScriptVariableGlobal target && target.Name == "attributes")
                {
                    var attributeName = node.Member.Name;
                    AttributeNames.Add(attributeName);
                }
                base.Visit(node);
            }

            public override void Visit(ScriptNode node)
            {
                base.Visit(node);
            }

            public override void Visit(ScriptIndexerExpression node)
            {
                if (node.Target is ScriptVariableGlobal target && target.Name == "attributes")
                {
                    if (node.Index is ScriptLiteral index && index.Value is string attributeName)
                    {
                        AttributeNames.Add(attributeName);
                    }
                    // TODO: what if the access is more complex? tackle those cases too
                }
                base.Visit(node);
            }
        }

        public static GeneratorAttributeValue Build(string templateStr)
        {
            var lexerOptions = new LexerOptions() { Lang = ScriptLang.Default, Mode = ScriptMode.ScriptOnly };
            var template = Template.Parse(templateStr, lexerOptions: lexerOptions);

            if (template.HasErrors)
            {
                throw new Exception($"Could not build generator attribute template because of parsing error(s):\n {string.Join("\n", template.Messages.Select(t => t.Message))}");
            }

            var visitor = new AttributeNameScriptVisitor();
            visitor.Visit(template.Page);

            return new GeneratorAttributeValue(template, templateStr, new HashSet<string>(visitor.AttributeNames));
        }
    }

    public interface IGeneratorSelection
    {
        IEnumerable<GeneratorV1> Filter(IEnumerable<GeneratorV1> generators);
    }

    public class GeneratorSelectionAll : IGeneratorSelection
    {
        public IEnumerable<GeneratorV1> Filter(IEnumerable<GeneratorV1> generators) => generators;
    }


    public interface IEffectiveGeneratorProvider
    {
        Task<IEnumerable<GeneratorV1>[]> GetEffectiveGenerators(string[] layerIDs, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold timeThreshold);
    }

    public class EffectiveGeneratorProvider : IEffectiveGeneratorProvider
    {
        private readonly IGeneratorModel generatorModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly ILayerModel layerModel;

        public EffectiveGeneratorProvider(IGeneratorModel generatorModel, IBaseConfigurationModel baseConfigurationModel, ILayerModel layerModel)
        {
            this.generatorModel = generatorModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.layerModel = layerModel;
        }

        public async Task<IEnumerable<GeneratorV1>[]> GetEffectiveGenerators(string[] layerIDs, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO, NOTE: we assume we get the layers back just as we queried for them, does this hold all the time?
            // TODO: rewrite GetLayers() to return array
            var layers = await layerModel.GetLayers(layerIDs, trans); // TODO: this should actually get the layers at the correct point in time, not the latest!

            var ret = new IEnumerable<GeneratorV1>[layerIDs.Length];
            IDictionary<string, GeneratorV1>? availableGenerators = null;
            Entity.Config.BaseConfigurationV1? baseConfiguration = null;
            int i = 0;
            foreach(var layer in layers)
            {
                var layerID = layer.ID;

                var l = new List<GeneratorV1>();
                ret[i++] = l;

                // check if this layer even has any active generators configured, if not -> return early
                var activeGeneratorIDsForLayer = layer.Generators;
                if (activeGeneratorIDsForLayer.IsEmpty())
                    continue;

                // NOTE: this is an important mechanism that prevents layers in the base configuration layerset form having effective generators
                // this is necessary, because otherwise its very easy to get infinite loops of GetGenerators() -> GetAttributes() -> GetGenerators() -> ...
                baseConfiguration ??= await baseConfigurationModel.GetConfigOrDefault(trans); // TODO: get base configuration at the correct point in time, not the latest
                if (baseConfiguration.ConfigLayerset.Contains(layerID))
                    continue;

                availableGenerators ??= await generatorModel.GetGenerators(new LayerSet(baseConfiguration.ConfigLayerset), trans, timeThreshold);

                var applicableGenerators = activeGeneratorIDsForLayer.Select(id => availableGenerators.GetOrWithClass(id, null)).Where(g => g != null).Select(g => g!);

                var filteredApplicableGenerators = generatorSelection.Filter(applicableGenerators);
                var filteredItems = FilterGeneratorsByAttributeSelection(filteredApplicableGenerators, attributeSelection);
                l.AddRange(filteredItems);
            }
            return ret;
        }

        private IEnumerable<GeneratorV1> FilterGeneratorsByAttributeSelection(IEnumerable<GeneratorV1> generators, IAttributeSelection attributeSelection)
        {
            foreach (var generator in generators)
                if (attributeSelection.Contains(generator.AttributeName))
                    yield return generator;
        }
    }

    public class GeneratorAttributeResolver
    {
        public CIAttribute? Resolve(IEnumerable<CIAttribute> existingAttributes, IEnumerable<CIAttribute>? additionalAttributes, Guid ciid, string layerID, GeneratorV1 generator)
        {
            try
            {
                var relevantAttributes = existingAttributes.Concat(additionalAttributes ?? new CIAttribute[0]).Where(a => generator.Value.UsedAttributeNames.Contains(a.Name)).ToList();
                if (relevantAttributes.Count == generator.Value.UsedAttributeNames.Count) 
                {
                    var context = ScribanVariableService.CreateAttributesBasedTemplateContext(relevantAttributes);

                    string templateSegment = generator.Value.Template.Render(context);

                    var value = new AttributeScalarValueText(templateSegment);
                    // create a deterministic, dependent guid from the ciid, layerID, attribute values; 
                    // we need to incorporate the dependent attributes, otherwise the attribute ID does not change when any of the dependent attributes change
                    var agGuid = GuidUtility.Create(ciid, $"{generator.AttributeName}-{layerID}-{string.Join("-", generator.Value.UsedAttributeNames)}");
                    Guid staticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "generator"); // TODO
                    var ag = new CIAttribute(agGuid, generator.AttributeName, ciid, value, AttributeState.New, staticChangesetID);
                    return ag;
                } else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
