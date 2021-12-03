using GraphQL;
using GraphQL.Execution;
using GraphQL.Language.AST;

namespace Omnikeeper.GraphQL
{
    public class MyDocumentExecutor : DocumentExecuter
    {
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            return context.Operation.OperationType switch
            {
                OperationType.Query => SerialExecutionStrategy.Instance,
                _ => base.SelectExecutionStrategy(context)
            };
        }
    }
}
