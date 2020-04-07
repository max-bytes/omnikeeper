using GraphQL;
using GraphQL.Execution;
using GraphQL.Language.AST;

namespace LandscapeRegistry.Utils
{
    public class MyDocumentExecutor : DocumentExecuter
    {
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            // TODO: Should we use cached instances of the default execution strategies?
            return context.Operation.OperationType switch
            {
                OperationType.Query => new SerialExecutionStrategy(),
                _ => base.SelectExecutionStrategy(context)
            };
        }
    }
}
