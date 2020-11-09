using GraphQL.Types;
using GraphQL.Utilities;
using System;

namespace Omnikeeper.GraphQL
{
    public class GraphQLSchema : Schema
    {
        public GraphQLSchema(IServiceProvider provider) : base(provider)
        {
            Query = provider.GetRequiredService<GraphQLQueryRoot>();
            Mutation = provider.GetRequiredService<GraphQLMutation>();
        }
    }
}
