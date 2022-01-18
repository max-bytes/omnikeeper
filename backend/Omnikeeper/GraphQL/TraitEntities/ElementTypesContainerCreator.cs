using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
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

        public ElementTypesContainer(ITrait trait, ElementType element, ElementWrapperType elementWrapper, IDInputType? iDInputType, TraitEntityRootType rootQueryType, UpsertInputType upsertInputType)
        {
            Trait = trait;
            Element = element;
            ElementWrapper = elementWrapper;
            IDInputType = iDInputType;
            RootQueryType = rootQueryType;
            UpsertInputType = upsertInputType;
        }
    }

    public class ElementTypesContainerCreator
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;

        public ElementTypesContainerCreator(ITraitsProvider traitsProvider, IAttributeModel attributeModel, IRelationModel relationModel, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService)
        {
            this.traitsProvider = traitsProvider;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
        }

        public IEnumerable<ElementTypesContainer> CreateTypes(IDictionary<string, ITrait> activeTraits, ISchema schema, ILogger logger)
        {
            var ret = new List<ElementTypesContainer>();
            foreach (var at in activeTraits)
            {
                if (at.Key == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                try
                {
                    var tt = new ElementType(at.Value, traitsProvider, dataLoaderService, ciModel);
                    var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, ciModel);
                    var idt = IDInputType.Build(at.Value);
                    var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, dataLoaderService, traitsProvider, attributeModel, relationModel, ttWrapper, idt);
                    var upsertInputType = new UpsertInputType(at.Value);

                    // TODO: needed?
                    //schema.RegisterTypes(upsertInputType, t);

                    ret.Add(new ElementTypesContainer(at.Value, tt, ttWrapper, idt, t, upsertInputType));
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create types for trait entity with trait ID {at.Key}");
                }
            }
            return ret;
        }

    }
}
