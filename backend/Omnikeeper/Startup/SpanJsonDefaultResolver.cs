using SpanJson.Resolvers;

namespace Omnikeeper.Startup
{
    public class SpanJsonDefaultResolver<TSymbol> : ResolverBase<TSymbol, SpanJsonDefaultResolver<TSymbol>> where TSymbol : struct
    {
        public SpanJsonDefaultResolver() : base(new SpanJsonOptions
        {
            NullOption = NullOptions.IncludeNulls,
            NamingConvention = NamingConventions.CamelCase,
            EnumOption = EnumOptions.String
        })
        {
        }
    }
}
