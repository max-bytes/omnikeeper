using GraphQL.Types;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL.TraitEntities
{
    public class TraitEntitiesQuerySchemaLoader
    {
        private readonly TraitEntitiesType tet;

        public TraitEntitiesQuerySchemaLoader(TraitEntitiesType tet)
        {
            this.tet = tet;
        }

        public void Init(IEnumerable<ElementTypesContainer> typesContainers)
        {
            foreach (var typeContainer in typesContainers)
            {
                var traitID = typeContainer.Trait.ID;

                var t = typeContainer.RootQueryType;

                var fieldName = TraitEntityTypesNameGenerator.GenerateTraitIDFieldName(traitID);

                tet.Field(fieldName, t, resolve: context => t);
            }
        }
    }
}
