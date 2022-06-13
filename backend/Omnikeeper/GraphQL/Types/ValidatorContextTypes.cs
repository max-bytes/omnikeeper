using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL.Types
{
    public class ValidatorContextType : ObjectGraphType<ValidatorContextV1>
    {
        public ValidatorContextType()
        {
            Field("id", x => x.ID);
            Field("validatorReference", x => x.ValidatorReference);
            Field("config", x => x.Config.RootElement.ToString());
        }
    }
}
