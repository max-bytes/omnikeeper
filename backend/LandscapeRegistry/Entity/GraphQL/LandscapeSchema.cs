using GraphQL.Types;
using GraphQL.Utilities;
using System;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class LandscapeSchema : Schema
    {
        public LandscapeSchema(IServiceProvider provider) : base(provider)
        {
            Query = provider.GetRequiredService<LandscapeQuery>();
            Mutation = provider.GetRequiredService<LandscapeMutation>();
        }
    }
}
