using GraphQL.Types;
using GraphQL.Utilities;
using System;

namespace LandscapeRegistry.GraphQL
{
    public class RegistrySchema : Schema
    {
        public RegistrySchema(IServiceProvider provider) : base(provider)
        {
            Query = provider.GetRequiredService<RegistryQuery>();
            Mutation = provider.GetRequiredService<RegistryMutation>();
        }
    }
}
