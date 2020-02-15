using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeSchema : Schema
    {
        public LandscapeSchema(IDependencyResolver resolver) : base(resolver)
        {
            Query = resolver.Resolve<LandscapeQuery>();
        }
    }
}
