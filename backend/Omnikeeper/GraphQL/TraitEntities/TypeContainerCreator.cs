﻿using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using System;
using System.Collections.Generic;

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
        public readonly UpsertInputType UpdateInput;
        public readonly FilterInputType FilterInput;
        public readonly TraitEntityModel TraitEntityModel;

        public ElementTypesContainer(ITrait trait, ElementType element, ElementWrapperType elementWrapper, IDInputType? iDInputType,
            TraitEntityRootType rootQueryType, UpsertInputType upsertInputType, UpsertInputType updateInputType, FilterInputType filterInputType, TraitEntityModel traitEntityModel)
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
        private readonly ICIIDModel ciidModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;
        private readonly IChangesetModel changesetModel;

        public TypeContainerCreator(ITraitsProvider traitsProvider, IAttributeModel attributeModel, IRelationModel relationModel, ICIIDModel ciidModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, IChangesetModel changesetModel)
        {
            this.traitsProvider = traitsProvider;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.ciidModel = ciidModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
            this.changesetModel = changesetModel;
        }

        public TypeContainer CreateTypes(IDictionary<string, ITrait> activeTraits, ILogger logger)
        {
            var relatedCIType = new RelatedCIType(traitsProvider, dataLoaderService);

            var elementTypesContainerDictionary = new Dictionary<string, ElementTypesContainer>();
            var filterInputTypesContainerDictionary = new Dictionary<string, FilterInputType>();

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
                    filterInputTypesContainerDictionary.Add(at.Key, filterInputType);
                    var idt = IDInputType.Build(at.Value);
                    var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel, dataLoaderService, ttWrapper, filterInputType, idt);
                    var upsertInputType = new UpsertInputType(at.Value, false);
                    var updateInputType = new UpsertInputType(at.Value, true);

                    var container = new ElementTypesContainer(at.Value, tt, ttWrapper, idt, t, upsertInputType, updateInputType, filterInputType, traitEntityModel);
                    elementTypesContainerDictionary.Add(at.Key, container);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create types for trait entity with trait ID {at.Key}");
                }
            }

            // we do a delayed initialization of the FilterInputType to be able to resolve trait hints linking to other FilterInputTypes
            foreach(var kv in filterInputTypesContainerDictionary)
            {
                try
                {
                    kv.Value.Init(filterInputTypesContainerDictionary);
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
