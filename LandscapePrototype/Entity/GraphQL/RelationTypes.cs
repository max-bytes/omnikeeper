using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(CIModel ciModel, LayerModel layerModel)
        {
            Field(x => x.ActivationTime);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.LayerID);
            Field(x => x.Predicate);
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);
            Field<CIType>("to",
            arguments: new QueryArguments(new List<QueryArgument>
            {
                new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
            }),
            resolve: (context) =>
            {
                var CIIdentity = ciModel.GetIdentityFromCIID(context.Source.ToCIID);
                var layerStrings = context.GetArgument<string[]>("layers");
                var layers = layerModel.BuildLayerSet(layerStrings);
                return ciModel.GetCI(CIIdentity, layers);
            });
        }
    }

    public class RelationStateType : EnumerationGraphType<RelationState>
    {
    }

    public class RelatedCIType : ObjectGraphType<RelatedCI>
    {
        public RelatedCIType()
        {
            Field(x => x.Relation, type: typeof(RelationType));
            Field("ci", x => x.CI, type: typeof(CIType));
        }
    }

}
