using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class ElementTypesContainer
    {
        public readonly ITrait Trait;
        public readonly ElementType Element;
        public readonly ElementWrapperType ElementWrapper;
        public readonly TraitEntityRootType RootQuery;
        public readonly IDInputType? IDInput;
        public readonly UpsertInputType UpsertInput;
        public readonly UpdateInputType UpdateInput;
        public readonly FilterInputType FilterInput;
        public readonly TraitEntityModel TraitEntityModel;

        public ElementTypesContainer(ITrait trait, ElementType element, ElementWrapperType elementWrapper, IDInputType? iDInputType,
            TraitEntityRootType rootQueryType, UpsertInputType upsertInputType, UpdateInputType updateInputType, FilterInputType filterInputType, TraitEntityModel traitEntityModel)
        {
            Trait = trait;
            Element = element;
            ElementWrapper = elementWrapper;
            IDInput = iDInputType;
            RootQuery = rootQueryType;
            UpsertInput = upsertInputType;
            UpdateInput = updateInputType;
            FilterInput = filterInputType;
            TraitEntityModel = traitEntityModel;
        }
    }

    public class TypeContainer
    {
        public readonly IEnumerable<ElementTypesContainer> ElementTypes;
        public readonly MergedCI2TraitEntityWrapper MergedCI2TraitEntityWrapper;

        public TypeContainer(IEnumerable<ElementTypesContainer> elementTypes, MergedCI2TraitEntityWrapper mergedCI2TraitEntityWrapper)
        {
            ElementTypes = elementTypes;
            MergedCI2TraitEntityWrapper = mergedCI2TraitEntityWrapper;
        }
    }

    public class TypeContainerCreator
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;
        private readonly IChangesetModel changesetModel;

        public TypeContainerCreator(ITraitsProvider traitsProvider, IAttributeModel attributeModel, IRelationModel relationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, IChangesetModel changesetModel)
        {
            this.traitsProvider = traitsProvider;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
            this.changesetModel = changesetModel;
        }

        public TypeContainer CreateTypes(IDictionary<string, ITrait> activeTraits, ILogger logger)
        {
            var relatedCIType = new RelatedCIType(traitsProvider, dataLoaderService);

            var elementTypesContainerDictionary = new Dictionary<string, ElementTypesContainer>();
            var filterInputTypesDictionary = new Dictionary<string, (FilterInputType filter, ITrait trait)>();

            foreach (var at in activeTraits)
            {
                if (at.Key == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                try
                {
                    var traitEntityModel = new TraitEntityModel(at.Value, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);

                    var tt = new ElementType();
                    var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, traitEntityModel);
                    var filterInputType = new FilterInputType(at.Value);
                    filterInputTypesDictionary.Add(at.Key, (filterInputType, at.Value));
                    var idt = IDInputType.Build(at.Value);
                    var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel, dataLoaderService, ttWrapper, filterInputType, idt);
                    var upsertInputType = new UpsertInputType(at.Value);
                    var updateInputType = new UpdateInputType(at.Value);

                    var container = new ElementTypesContainer(at.Value, tt, ttWrapper, idt, t, upsertInputType, updateInputType, filterInputType, traitEntityModel);
                    elementTypesContainerDictionary.Add(at.Key, container);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create types for trait entity with trait ID {at.Key}");
                }
            }

            // we do a delayed initialization of the FilterInputType to be able to resolve trait hints linking to other FilterInputTypes
            var traitRelationFilterWrapperDictionary = filterInputTypesDictionary.ToDictionary(kv => kv.Key, kv => (new TraitRelationFilterWrapperType(kv.Value.filter, kv.Value.trait), kv.Value.trait));
            foreach (var kv in filterInputTypesDictionary)
            {
                try
                {
                    kv.Value.filter.LateInit(traitRelationFilterWrapperDictionary);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create filter input type for trait entity with trait ID {kv.Key}");
                }
            }

            // we do a delayed initialization of the ElementType to be able to resolve element wrappers
            foreach (var kv in elementTypesContainerDictionary)
            {
                try
                {
                    var trait = activeTraits[kv.Key];
                    kv.Value.Element.Init(trait, relatedCIType, elementTypesContainerDictionary.TryGetValue, dataLoaderService, traitsProvider, logger);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create types for trait entity with trait ID {kv.Key}");
                }
            }

            var w = new MergedCI2TraitEntityWrapper(elementTypesContainerDictionary.Values, dataLoaderService);

            return new TypeContainer(elementTypesContainerDictionary.Values, w);
        }

    }
}
