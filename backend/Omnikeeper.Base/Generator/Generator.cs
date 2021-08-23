using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Templating;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
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
        public Generator(LayerSet readLayerSet, GeneratorSelectorByTrait selector, IEnumerable<GeneratorItem> items)
        {
            ReadLayerSet = readLayerSet;
            Selector = selector;
            Items = items;
        }

        public LayerSet ReadLayerSet { get; }
        public GeneratorSelectorByTrait Selector { get; }
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
        public GeneratorAttributeValue(Scriban.Template template)
        {
            Template = template;
        }

        public Scriban.Template Template { get; }

        public static GeneratorAttributeValue Build(string templateStr)
        {
            return new GeneratorAttributeValue(Scriban.Template.Parse(templateStr));
        }
    }

    public interface IGeneratorSelection
    {
        bool Contains(Generator g);
        bool ContainsItem(GeneratorItem item);
    }

    public class GeneratorSelectionAll : IGeneratorSelection
    {
        public bool Contains(Generator g) => true;
        public bool ContainsItem(GeneratorItem item) => true;
    }

    public class GeneratorSelectionContainingFullItemName : IGeneratorSelection
    {
        private readonly string ItemName;

        public GeneratorSelectionContainingFullItemName(string itemName)
        {
            ItemName = itemName;
        }

        public bool Contains(Generator g)
        {
            return g.Items.Any(item => item.Name.Equals(ItemName));
        }

        public bool ContainsItem(GeneratorItem item)
        {
            return item.Name.Equals(ItemName);
        }
    }

    public class GeneratorSelectionContainingRegexItemName : IGeneratorSelection
    {
        private readonly Regex regexItemName;

        public GeneratorSelectionContainingRegexItemName(string regexItemNamePattern)
        {
            regexItemName = new Regex(regexItemNamePattern);
        }

        public bool Contains(Generator g)
        {
            return g.Items.Any(item => regexItemName.IsMatch(item.Name));
        }

        public bool ContainsItem(GeneratorItem item)
        {
            return regexItemName.IsMatch(item.Name);
        }
    }



    public interface IEffectiveGeneratorProvider
    {
        Task<IEnumerable<(GeneratorItem generatorItem, MergedCI ci)>> GetEffectiveGeneratorItems(string layerID, ICIIDSelection selection, IGeneratorSelection generatorSelection, IModelContext mc, TimeThreshold timeThreshold);
    }

    public class EffectiveGeneratorProvider : IEffectiveGeneratorProvider
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;

        public EffectiveGeneratorProvider(ITraitsProvider traitsProvider, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel)
        {
            this.traitsProvider = traitsProvider;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
        }

        public async Task<IEnumerable<(GeneratorItem generatorItem, MergedCI ci)>> GetEffectiveGeneratorItems(string layerID, ICIIDSelection selection, IGeneratorSelection generatorSelection, IModelContext mc, TimeThreshold timeThreshold)
        {
            // setup, TODO: move
            var generators = new Dictionary<string, Generator>()
            {
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
                //{
                //    2,
                //    new List<Generator>()
                //    {
                //        generators["host_set_name_from_hostname"]
                //    }
                //}
            };

            var effectiveGeneratorItems = new List<(GeneratorItem, MergedCI)>();
            if (appliedGenerators.TryGetValue(layerID, out var applicableGenerators))
            {
                var filteredApplicableGenerators = applicableGenerators.Where(ag =>
                {
                    return generatorSelection.Contains(ag);
                });

                foreach (var g in filteredApplicableGenerators)
                {
                    var items = g.Items.Where(item => generatorSelection.ContainsItem(item));

                    if (!items.IsEmpty())
                    {
                        var traitName = g.Selector.TraitName;
                        var trait = await traitsProvider.GetActiveTrait(traitName, mc, timeThreshold);
                        if (trait == null)
                            continue; // a trait that is not found can never apply

                        var cis = await ciModel.GetMergedCIs(selection, g.ReadLayerSet, false, mc, timeThreshold); // TODO: group by layerset to cut down on calls to GetMergedCIs
                        foreach (var ci in cis)
                        {
                            // check if generator applies
                            var applies = await effectiveTraitModel.DoesCIHaveTrait(ci, trait, mc, timeThreshold);

                            if (applies)
                            {
                                foreach (var item in items)
                                    effectiveGeneratorItems.Add((item, ci));
                            }
                        }
                    }
                }
            }

            return effectiveGeneratorItems;
        }
    }

    public class GeneratorAttributeResolver
    {
        public CIAttribute? Resolve(MergedCI mergedCI, string layerID, GeneratorItem item)
        {
            try
            {
                var context = ScribanVariableService.CreateSimpleCIBasedTemplateContext(mergedCI);
                string templateSegment = item.Value.Template.Render(context);

                var value = new AttributeScalarValueText(templateSegment);
                // create a deterministic, dependent guid from the ciid, layerID, attribute name; 
                // TODO: is this correct? NO! we need to incorporate the dependent attributes, otherwise the attribute ID does not change when any of the dependent attributes change
                // how do we get them? probably by parsing the template and extracting the accessed attributes?
                var agGuid = GuidUtility.Create(mergedCI.ID, $"{item.Name}-{layerID}");
                Guid staticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "generator"); // TODO
                var ag = new CIAttribute(agGuid, item.Name, mergedCI.ID, value, AttributeState.New, staticChangesetID);
                return ag;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
