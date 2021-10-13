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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

//new RecursiveTrait("__meta.config.generator", new TraitOriginV1(TraitOriginType.Core),
//            new List<TraitAttribute>() {
//                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("generator.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
//                new TraitAttribute("attribute_name", CIAttributeTemplate.BuildFromParams("generator.attribute_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
//                new TraitAttribute("attribute_value_template", CIAttributeTemplate.BuildFromParams("generator.attribute_value_template", AttributeValueType.MultilineText, false)),
//            },
//            new List<TraitAttribute>()
//            {
//                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
//            }
//        )

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

        public static Guid StaticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "generator"); // TODO

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
            // TODO, NOTE: we assume we get the layers back just as we queried for them, does this hold all the time?
            // TODO: rewrite GetLayers() to return array
            var layers = (await layerModel.GetLayers(layerIDs, trans)).ToDictionary(l => l.ID); // TODO: this should actually get the layers at the correct point in time, not the latest!

            var ret = new IEnumerable<GeneratorV1>[layerIDs.Length];
            IDictionary<string, GeneratorV1>? availableGenerators = null;
            MetaConfiguration? metaConfiguration = null;
            int i = 0;
            foreach(var layerID in layerIDs)
            {
                var l = new List<GeneratorV1>();
                ret[i++] = l;

                if (layers.TryGetValue(layerID, out var layer))
                {
                    // check if this layer even has any active generators configured, if not -> return early
                    var activeGeneratorIDsForLayer = layer.Generators;
                    if (activeGeneratorIDsForLayer.IsEmpty())
                        continue;

                    // NOTE: this is an important mechanism that prevents layers in the base configuration layerset form having effective generators
                    // this is necessary, because otherwise its very easy to get infinite loops of GetGenerators() -> GetAttributes() -> GetGenerators() -> ...
                    metaConfiguration ??= await metaConfigurationModel.GetConfigOrDefault(trans); // TODO: get base configuration at the correct point in time, not the latest
                    if (metaConfiguration.ConfigLayerset.Contains(layerID))
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
                if (relevantAttributes.Count() == generator.Template.UsedAttributeNames.Count()) 
                {
                    var context = ScribanVariableService.CreateAttributesBasedTemplateContext(relevantAttributes);

                    string templateSegment = generator.Template.Template.Render(context);

                    var value = new AttributeScalarValueText(templateSegment);
                    // create a deterministic, dependent guid from the ciid, layerID, attribute values; 
                    // we need to incorporate the dependent attributes, otherwise the attribute ID does not change when any of the dependent attributes change
                    var agGuid = GuidUtility.Create(ciid, $"{generator.AttributeName}-{layerID}-{string.Join("-", relevantAttributes.Select(a => a.ID))}");
                    var ag = new CIAttribute(agGuid, generator.AttributeName, ciid, value, GeneratorV1.StaticChangesetID);
                    return ag;
                } else
                {
                    return null; // TODO: better error handling
                }
            }
            catch (Exception e)
            {
                return null; // TODO: better error handling
            }
        }
    }
}
