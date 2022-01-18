using GraphQL.Types;
using Omnikeeper.GraphQL.Types;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class TraitEntitiesQuerySchemaLoader
    {
        private readonly TraitEntitiesType tet;
        private readonly MergedCIType mergedCIType;

        public TraitEntitiesQuerySchemaLoader(TraitEntitiesType tet, MergedCIType mergedCIType)
        {
            this.tet = tet;
            this.mergedCIType = mergedCIType;
        }

        public void Init(TypeContainer typeContainer)
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
