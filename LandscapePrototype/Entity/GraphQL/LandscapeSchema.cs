using GraphQL;
using GraphQL.Types;
using GraphQL.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeSchema : Schema
    {
        public LandscapeSchema(IServiceProvider provider) : base(provider)
        {
            Query = provider.GetRequiredService<LandscapeQuery>();
            Mutation = provider.GetRequiredService<CIMutation>();
        }
    }
}
