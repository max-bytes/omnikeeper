using GraphQL.Types;
using LandscapePrototype.Model;

namespace LandscapePrototype.Entity.GraphQL
{
    public class CIMutation : ObjectGraphType
    {
        public CIMutation(CIModel ciModel)
        {
            Field<LongGraphType>("createCI",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CIInputType>> { Name = "identity" }
              ),
              resolve: context =>
              {
                  var ci = context.GetArgument<CI>("identity");
                  return ciModel.CreateCI(ci.Identity);
              });
        }
    }
}
