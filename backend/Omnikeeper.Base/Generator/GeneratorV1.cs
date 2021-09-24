using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Templating;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Scriban;
using Scriban.Parsing;
using Scriban.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private GeneratorAttributeValue(Template template, ISet<string> usedAttributeNames)
        {
            Template = template;
            UsedAttributeNames = usedAttributeNames;
        }

        public Template Template { get; }
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

            return new GeneratorAttributeValue(template, new HashSet<string>(visitor.AttributeNames));
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
        IEnumerable<GeneratorV1> GetEffectiveGenerators(string layerID, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection);
    }

    public class EffectiveGeneratorProvider : IEffectiveGeneratorProvider
    {
        public IEnumerable<GeneratorV1> GetEffectiveGenerators(string layerID, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection)
        {
            // setup, TODO: move
            var generators = new Dictionary<string, GeneratorV1>()
            {
                {"generator_test_01",  new GeneratorV1("generator_test_01", "generated_attribute", GeneratorAttributeValue.Build("attributes.hostname|string.upcase")) }
                //{ "host_set_name_from_hostname", new Generator(new LayerSet(1), new GeneratorSelectorByTrait("host"), new List<GeneratorItem>()
                //    {
                //        new GeneratorItem(ICIModel.NameAttribute, GeneratorAttributeValue.Build("{{ a.hostname|string.upcase }}"))
                //    }) 
                //}
            };

            // TODO: make sure applied generators are valid and do not read from themselves
            // setup, TODO: move
            var appliedGenerators = new Dictionary<string, List<GeneratorV1>>
            {
                {
                    "testlayer01",
                    new List<GeneratorV1>()
                    {
                        generators["generator_test_01"]
                    }
                }
            };

            if (appliedGenerators.TryGetValue(layerID, out var applicableGenerators))
            {
                var filteredApplicableGenerators = generatorSelection.Filter(applicableGenerators);

                var filteredItems = FilterGeneratorsByAttributeSelection(filteredApplicableGenerators, attributeSelection);
                foreach (var generator in filteredItems)
                    yield return generator;
            }
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
