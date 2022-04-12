using GraphQL;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace Omnikeeper.GraphQL
{
    public class MyDocumentExecutor : DocumentExecuter
    {
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            return context.Operation.Operation switch
            {
                OperationType.Query => SerialExecutionStrategy.Instance,
                _ => base.SelectExecutionStrategy(context)
            };
        }
    }
}
