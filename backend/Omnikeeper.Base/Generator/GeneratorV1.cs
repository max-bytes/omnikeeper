using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
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
using Omnikeeper.Base.Model.TraitBased;

namespace Omnikeeper.Base.Generator
{
    [TraitEntity("__meta.config.generator", TraitOriginType.Core)]
    public class GeneratorV1 : TraitEntity, IEquatable<GeneratorV1>
    {
        public GeneratorV1(string id, string attributeName, string templateString)
        {
            ID = id;
            AttributeName = attributeName;
            TemplateString = templateString;
            Name = $"Generator - {ID}";
        }

        public GeneratorV1() { ID = ""; AttributeName = ""; TemplateString = ""; Name = ""; }

        [TraitAttribute("id", "generator.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(IDValidations.GeneratorIDRegexString, IDValidations.GeneratorIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("attribute_name", "generator.attribute_name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string AttributeName;

        [TraitAttribute("attribute_value_template", "generator.attribute_value_template", multilineTextHint: true)]
        public readonly string TemplateString;

        private GeneratorAttributeValue? _template = null;
        public GeneratorAttributeValue Template
        {
            get
            {
                if (_template == null)
                {
                    _template = GeneratorAttributeValue.Build(TemplateString);
                }
                return _template;
            }
        }

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public static readonly Guid StaticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "generator"); // TODO

        public override bool Equals(object? obj) => Equals(obj as GeneratorV1);
        public bool Equals(GeneratorV1? other)
        {
            return other != null && ID == other.ID &&
                   AttributeName == other.AttributeName &&
                   TemplateString == other.TemplateString &&
                   Name == other.Name;
        }
        public override int GetHashCode() => HashCode.Combine(ID, AttributeName, TemplateString, Name);
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
        private readonly GenericTraitEntityModel<GeneratorV1, string> generatorModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ILayerModel layerModel;

        public EffectiveGeneratorProvider(GenericTraitEntityModel<GeneratorV1, string> generatorModel, IMetaConfigurationModel metaConfigurationModel, ILayerModel layerModel)
        {
            this.generatorModel = generatorModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.layerModel = layerModel;
        }

        public async Task<IEnumerable<GeneratorV1>[]> GetEffectiveGenerators(string[] layerIDs, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold timeThreshold)
        {
            Dictionary<string, Layer>? layers = null;

            var ret = new IEnumerable<GeneratorV1>[layerIDs.Length];
            IDictionary<string, GeneratorV1>? availableGenerators = null;
            MetaConfiguration? metaConfiguration = null;
            int i = 0;
            foreach(var layerID in layerIDs)
            {
                var l = new List<GeneratorV1>();
                ret[i++] = l;

                // NOTE: this is an important mechanism that prevents layers in the base configuration layerset form having effective generators
                // this is necessary, because otherwise its very easy to get infinite loops of GetGenerators() -> GetAttributes() -> GetGenerators() -> ...
                // NOTE: to be 100% consistent, we SHOULD get the base configuration at the correct point in time, not the latest... but the meta-configuration is not stored historically
                // so we have to live with this inconsistency; it's shouldn't affect much anyway, because changing the meta configuration is really rare in practice
                metaConfiguration ??= await metaConfigurationModel.GetConfigOrDefault(trans);
                if (metaConfiguration.ConfigLayerset.Contains(layerID))
                    continue;

                layers ??= (await layerModel.GetLayers(layerIDs, trans, timeThreshold)).ToDictionary(l => l.ID);

                if (layers.TryGetValue(layerID, out var layer))
                {
                    // check if this layer even has any active generators configured, if not -> return early
                    var activeGeneratorIDsForLayer = layer.Generators;
                    if (activeGeneratorIDsForLayer.IsEmpty())
                        continue;

                    availableGenerators ??= await generatorModel.GetAllByDataID(metaConfiguration.ConfigLayerset, trans, timeThreshold);

                    var applicableGenerators = activeGeneratorIDsForLayer.Select(id => availableGenerators.GetOrWithClass(id, null)).Where(g => g != null).Select(g => g!);

                    var filteredApplicableGenerators = generatorSelection.Filter(applicableGenerators);
                    var filteredItems = FilterGeneratorsByAttributeSelection(filteredApplicableGenerators, attributeSelection);
                    l.AddRange(filteredItems);
                }
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
                var relevantAttributes = existingAttributes.Concat(additionalAttributes ?? new CIAttribute[0]).Where(a => generator.Template.UsedAttributeNames.Contains(a.Name)).ToList();
                var context = ScribanVariableService.CreateAttributesBasedTemplateContext(relevantAttributes);

                object evaluated = generator.Template.Template.Evaluate(context);

                if (evaluated is string evaluatedString)
                {
                    var value = new AttributeScalarValueText(evaluatedString);
                    // create a deterministic, dependent guid from the ciid, layerID, attribute values; 
                    // we need to incorporate the dependent attributes, otherwise the attribute ID does not change when any of the dependent attributes change
                    var agGuid = GuidUtility.Create(ciid, $"{generator.AttributeName}-{layerID}-{string.Join("-", relevantAttributes.Select(a => a.ID))}");
                    var ag = new CIAttribute(agGuid, generator.AttributeName, ciid, value, GeneratorV1.StaticChangesetID);
                    return ag;
                } else if (evaluated is null)
                {
                    return null;
                } else
                {
                    // TODO: better error handling, not supported return detected
                    return null;
                }
            }
            catch (Exception)
            {
                return null; // TODO: better error handling
            }
        }
    }
}
