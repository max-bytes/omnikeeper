using GraphQL.Types;
using Omnikeeper.Base.Generator;

namespace Omnikeeper.GraphQL
{
    public class GeneratorType : ObjectGraphType<GeneratorV1>
    {
        public GeneratorType()
        {
            Field("id", x => x.ID);
            Field("attributeName", x => x.AttributeName);
            Field("attributeValueTemplate", x => x.TemplateString);
        }
    }
}
