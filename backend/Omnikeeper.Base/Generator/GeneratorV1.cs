﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
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
        public string ID;

        [TraitAttribute("attribute_name", "generator.attribute_name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string AttributeName;

        [TraitAttribute("attribute_value_template", "generator.attribute_value_template", multilineTextHint: true)]
        public string TemplateString;

        // TODO: better caching of templates, currently they are created new at each request
        private GeneratorAttributeValue? _template = null;
        public GeneratorAttributeValue Template
        {
            get
            {
                _template ??= GeneratorAttributeValue.Build(TemplateString);
                return _template;
            }
        }

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

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
        private readonly GeneratorV1Model generatorModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly ILayerDataModel layerDataModel;

        public EffectiveGeneratorProvider(GeneratorV1Model generatorModel, IMetaConfigurationModel metaConfigurationModel, ILayerDataModel layerDataModel)
        {
            this.generatorModel = generatorModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.layerDataModel = layerDataModel;
        }

        public async Task<IEnumerable<GeneratorV1>[]> GetEffectiveGenerators(string[] layerIDs, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold timeThreshold)
        {
            IDictionary<string, LayerData>? layerData = null;

            var ret = new IEnumerable<GeneratorV1>[layerIDs.Length];
            IDictionary<string, GeneratorV1>? availableGenerators = null;
            MetaConfiguration? metaConfiguration = null;
            int i = 0;
            foreach (var layerID in layerIDs)
            {
                var l = new List<GeneratorV1>();
                ret[i++] = l;

                // NOTE: this is an important mechanism that prevents layers in the base configuration layerset form having effective generators
                // this is necessary, because otherwise its very easy to get infinite loops of GetGenerators() -> GetAttributes() -> GetGenerators() -> ...
                // NOTE: to be 100% consistent, we SHOULD get the meta configuration at the correct point in time, not the latest... but the meta-configuration is not stored historically
                // so we have to live with this inconsistency; it's shouldn't affect much anyway, because changing the meta configuration is really rare in practice
                metaConfiguration ??= await metaConfigurationModel.GetConfigOrDefault(trans);
                if (metaConfiguration.ConfigLayerset.Contains(layerID))
                    continue;

                layerData ??= await layerDataModel.GetLayerData(trans, timeThreshold);

                if (layerData.TryGetValue(layerID, out var ld))
                {
                    // check if this layer even has any active generators configured, if not -> return early
                    var activeGeneratorIDsForLayer = ld.Generators;
                    if (activeGeneratorIDsForLayer.IsEmpty())
                        continue;

                    availableGenerators ??= await generatorModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, timeThreshold);

                    var applicableGenerators = activeGeneratorIDsForLayer.Select(id => availableGenerators.GetOrWithClass(id, null)).Where(g => g != null).Select(g => g!);

                    var filteredApplicableGenerators = generatorSelection.Filter(applicableGenerators);

                    foreach (var generator in filteredApplicableGenerators)
                        if (attributeSelection.ContainsAttributeName(generator.AttributeName))
                            l.Add(generator);
                }
            }
            return ret;
        }
    }

    public static class GeneratorAttributeResolver
    {
        public static CIAttribute? Resolve(IDictionary<string, MergedCIAttribute> existingAttributes, Guid ciid, string layerID, GeneratorV1 generator)
        {
            try
            {
                var relevantAttributes = existingAttributes.Values
                    .Where(a => generator.Template.UsedAttributeNames.Contains(a.Attribute.Name))
                    .ToList();
                var context = ScribanVariableService.CreateAttributesBasedTemplateContext(relevantAttributes.ToDictionary(a => a.Attribute.Name, a => a.Attribute.Value.ToGenericObject()));

                object evaluated = generator.Template.Template.Evaluate(context);

                if (evaluated is string evaluatedString)
                {
                    var value = new AttributeScalarValueText(evaluatedString);
                    // create a deterministic, dependent guid from the ciid, layerID, attribute values; 
                    // we need to incorporate the dependent attributes, otherwise the attribute ID does not change when any of the dependent attributes change
                    // TODO: I *think* we also need a hash of the generator template, because otherwise, changes there are not reflected as new IDs
                    var agGuid = GuidUtility.Create(ciid, $"{generator.AttributeName}-{layerID}-{string.Join("-", relevantAttributes.Select(kv => kv.Attribute.ID))}");
                    var ag = new CIAttribute(agGuid, generator.AttributeName, ciid, value, GeneratorV1.StaticChangesetID);
                    return ag;
                }
                else if (evaluated is null)
                {
                    return null;
                }
                else
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
