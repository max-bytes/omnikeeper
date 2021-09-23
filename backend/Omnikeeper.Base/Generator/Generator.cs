using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Generator
{
    //public class AppliedGenerator
    //{
    //    public AppliedGenerator(Generator generator, long layerID)
    //    {
    //        Generator = generator;
    //        LayerID = layerID;
    //    }

    //    public Generator Generator { get; }
    //    public long LayerID { get; }
    //}

    public class Generator
    {
        public Generator(IEnumerable<GeneratorItem> items)
        {
            Items = items;
        }

        //public GeneratorSelectorByTrait Selector { get; }
        public IEnumerable<GeneratorItem> Items { get; }
    }

    public class GeneratorSelectorByTrait
    {
        public GeneratorSelectorByTrait(string traitName)
        {
            TraitName = traitName;
        }

        public string TraitName { get; }
    }

    public class GeneratorItem
    {
        public GeneratorItem(string name, GeneratorAttributeValue value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public GeneratorAttributeValue Value { get; }
    }

    public class GeneratorAttributeValue
    {
        private GeneratorAttributeValue(Scriban.Template template, ISet<string> usedAttributeNames)
        {
            Template = template;
            UsedAttributeNames = usedAttributeNames;
        }

        public Scriban.Template Template { get; }
        public ISet<string> UsedAttributeNames { get; }

        private class AttributeNameScriptVisitor : ScriptVisitor
        {
            public readonly ISet<string> AttributeNames = new HashSet<string>();
            //public override void Visit(ScriptVariableGlobal node)
            //{
            //    if (node.Name == "attributes")
            //    {
            //        Console.WriteLine("!");
            //    }
            //    base.Visit(node);
            //}

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
            var template = Scriban.Template.Parse(templateStr, lexerOptions: lexerOptions);

            var visitor = new AttributeNameScriptVisitor();
            visitor.Visit(template.Page);

            return new GeneratorAttributeValue(template, new HashSet<string>(visitor.AttributeNames));
        }
    }

    public interface IGeneratorSelection
    {
        IEnumerable<Generator> Filter(IEnumerable<Generator> generators);
        IEnumerable<GeneratorItem> FilterItems(IEnumerable<GeneratorItem> items);
    }

    public class GeneratorSelectionAll : IGeneratorSelection
    {
        public IEnumerable<Generator> Filter(IEnumerable<Generator> generators) => generators;
        public IEnumerable<GeneratorItem> FilterItems(IEnumerable<GeneratorItem> items) => items;
    }

    public class GeneratorSelectionContainingFullItemName : IGeneratorSelection
    {
        private readonly string ItemName;

        public GeneratorSelectionContainingFullItemName(string itemName)
        {
            ItemName = itemName;
        }

        public IEnumerable<Generator> Filter(IEnumerable<Generator> generators)
        {
            return generators.Where(ag => ag.Items.Any(item => item.Name.Equals(ItemName)));
        }
        public IEnumerable<GeneratorItem> FilterItems(IEnumerable<GeneratorItem> items)
        {
            return items.Where(item => item.Name.Equals(ItemName));
        }
    }


    public interface IEffectiveGeneratorProvider
    {
        IEnumerable<GeneratorItem> GetEffectiveGeneratorItems(string layerID, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection);
    }

    public class EffectiveGeneratorProvider : IEffectiveGeneratorProvider
    {
        public IEnumerable<GeneratorItem> GetEffectiveGeneratorItems(string layerID, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection)
        {
            // setup, TODO: move
            var generators = new Dictionary<string, Generator>()
            {
                { "generator_test_01", new Generator(new List<GeneratorItem>()
                    {
                        new GeneratorItem("generated_attribute", GeneratorAttributeValue.Build("attributes.hostname|string.upcase"))
                    })
                }
                //{ "host_set_name_from_hostname", new Generator(new LayerSet(1), new GeneratorSelectorByTrait("host"), new List<GeneratorItem>()
                //    {
                //        new GeneratorItem(ICIModel.NameAttribute, GeneratorAttributeValue.Build("{{ a.hostname|string.upcase }}"))
                //    }) 
                //}
            };

            // TODO: make sure applied generators are valid and do not read from themselves
            // setup, TODO: move
            var appliedGenerators = new Dictionary<string, List<Generator>>
            {
                {
                    "testlayer01",
                    new List<Generator>()
                    {
                        generators["generator_test_01"]
                    }
                }
            };

            //var effectiveGeneratorItems = new List<(GeneratorItem, MergedCI)>();
            if (appliedGenerators.TryGetValue(layerID, out var applicableGenerators))
            {
                var filteredApplicableGenerators = generatorSelection.Filter(applicableGenerators);
                
                foreach (var g in filteredApplicableGenerators)
                {
                    var filteredItems = generatorSelection.FilterItems(g.Items);
                    filteredItems = FilterItemsByAttributeSelection(filteredItems, attributeSelection);
                    foreach (var item in filteredItems)
                        yield return item;

                    //if (!items.IsEmpty())
                    //{
                    //    var traitName = g.Selector.TraitName;
                    //    var trait = await traitsProvider.GetActiveTrait(traitName, mc, timeThreshold);
                    //    if (trait == null)
                    //        continue; // a trait that is not found can never apply

                    //    var cisWithTrait = await effectiveTraitModel.GetMergedCIsWithTrait(trait, g.ReadLayerSet, selection, mc, timeThreshold);
                    //    foreach(var ciWithTrait in cisWithTrait)
                    //    {
                    //        foreach (var item in items)
                    //            effectiveGeneratorItems.Add((item, ciWithTrait));
                    //    }
                    //}
                }
            }

            //return effectiveGeneratorItems;
        }

        private IEnumerable<GeneratorItem> FilterItemsByAttributeSelection(IEnumerable<GeneratorItem> items, IAttributeSelection attributeSelection)
        {
            foreach (var item in items)
                if (attributeSelection.Contains(item.Name))
                    yield return item;
        }
    }

    public class GeneratorAttributeResolver
    {
        public CIAttribute? Resolve(IEnumerable<CIAttribute> existingAttributes, IEnumerable<CIAttribute>? additionalAttributes, Guid ciid, string layerID, GeneratorItem item)
        {
            try
            {
                var relevantAttributes = existingAttributes.Concat(additionalAttributes ?? new CIAttribute[0]).Where(a => item.Value.UsedAttributeNames.Contains(a.Name)).ToList();
                if (relevantAttributes.Count == item.Value.UsedAttributeNames.Count) 
                {
                    var context = ScribanVariableService.CreateAttributesBasedTemplateContext(relevantAttributes);

                    string templateSegment = item.Value.Template.Render(context);

                    var value = new AttributeScalarValueText(templateSegment);
                    // create a deterministic, dependent guid from the ciid, layerID, attribute values; 
                    // we need to incorporate the dependent attributes, otherwise the attribute ID does not change when any of the dependent attributes change
                    var agGuid = GuidUtility.Create(ciid, $"{item.Name}-{layerID}-{string.Join("-", relevantAttributes)}");
                    Guid staticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "generator"); // TODO
                    var ag = new CIAttribute(agGuid, item.Name, ciid, value, AttributeState.New, staticChangesetID);
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
