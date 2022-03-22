using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class ElementTypesContainer
    {
        public readonly ITrait Trait;
        public readonly ElementType Element;
        public readonly ElementWrapperType ElementWrapper;
        public readonly TraitEntityRootType RootQueryType;
        public readonly IDInputType? IDInputType;
        public readonly UpsertInputType UpsertInputType;

        public ElementTypesContainer(ITrait trait, ElementType element, ElementWrapperType elementWrapper, IDInputType? iDInputType,
            TraitEntityRootType rootQueryType, UpsertInputType upsertInputType)
        {
            Trait = trait;
            Element = element;
            ElementWrapper = elementWrapper;
            IDInputType = iDInputType;
            RootQueryType = rootQueryType;
            UpsertInputType = upsertInputType;
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
            var elementTypes = new List<ElementTypesContainer>();

            var relatedCIType = new RelatedCIType(traitsProvider, dataLoaderService, ciModel, attributeModel);

            var elementWrapperTypeDictionary = new Dictionary<string, ElementWrapperType>();
            var elementTypeDictionary = new Dictionary<string, ElementType>();

            foreach (var at in activeTraits)
            {
                if (at.Key == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                try
                {
                    var tt = new ElementType();
                    elementTypeDictionary.Add(at.Key, tt);
                    var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, ciModel, changesetModel, attributeModel);
                    elementWrapperTypeDictionary.Add(at.Key, ttWrapper);
                    var filterInputType = FilterInputType.Build(at.Value);
                    var idt = IDInputType.Build(at.Value);
                    var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, ciidModel, attributeModel, relationModel, dataLoaderService, ttWrapper, filterInputType, idt);
                    var upsertInputType = new UpsertInputType(at.Value);

                    elementTypes.Add(new ElementTypesContainer(at.Value, tt, ttWrapper, idt, t, upsertInputType));
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create types for trait entity with trait ID {at.Key}");
                }
            }

            // we do a delayed initialization of the ElementType to be able to resolve element wrappers
            foreach(var kv in elementTypeDictionary)
            {
                var trait = activeTraits[kv.Key];
                kv.Value.Init(trait, relatedCIType, (traitID) => elementWrapperTypeDictionary[traitID], dataLoaderService, effectiveTraitModel, traitsProvider, ciModel, attributeModel);
            }

            

            var w = new MergedCI2TraitEntityWrapper(elementTypes, dataLoaderService, effectiveTraitModel, traitsProvider);

            return new TypeContainer(elementTypes, w);
        }

    }
}
