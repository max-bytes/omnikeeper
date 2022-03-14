using GraphQL.Types;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.GraphQL.Types;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class TraitEntitiesQuerySchemaLoader
    {
        public void Init(MergedCIType mergedCIType, TraitEntitiesType tet, TypeContainer typeContainer)
        {
            foreach (var elementTypeContainer in typeContainer.ElementTypes)
            {
                var traitID = elementTypeContainer.Trait.ID;

                var fieldName = TraitEntityTypesNameGenerator.GenerateTraitIDFieldName(traitID);
                var t = elementTypeContainer.RootQueryType;
                tet.Field(fieldName, t, resolve: context => t);
            }

            var w = typeContainer.MergedCI2TraitEntityWrapper;
            mergedCIType.Field("traitEntity", w, resolve: context => w);
        }
    }

}
