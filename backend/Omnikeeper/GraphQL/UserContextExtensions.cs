using GraphQL;
using GraphQLParser.AST;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL
{
    public static class UserContextExtensions
    {
        public static OmnikeeperUserContext SetupUserContext(this IResolveFieldContext<object?> rfc)
        {
            return Setup(rfc.UserContext, rfc.Document);
        }

        public static OmnikeeperUserContext SetupUserContext(this IResolveFieldContext rfc)
        {
            return Setup(rfc.UserContext, rfc.Document);
        }

        private static OmnikeeperUserContext Setup(IDictionary<string, object?> untypeUserContext, GraphQLDocument document)
        {
            var userContext = (untypeUserContext as OmnikeeperUserContext)!;

            // find out, if...
            // ...we are in the first operation (query or mutation)
            // ...we are in a mutation,
            // then for mutations, set up an intermediate data structure that keeps track of mutations
            if (userContext.TryGetValue("multiQueryData", out var _))
            { // we are NOT in the first operation, because multiQueryData is already set
                // NO-OP
            } 
            else if (userContext.MultiMutationData != null)
            { // we are NOT in the first operation, because multiOperationData is already set
                userContext.MultiMutationData.IncrementOperationIndex();
            } 
            else
            { // we are in the first operation, build MultiOperationData
                if (document.Definitions[0] is GraphQLOperationDefinition od)
                {
                    switch (od.Operation)
                    {
                        case OperationType.Query:
                            userContext["multiQueryData"] = true;
                            break;
                        case OperationType.Mutation:
                            var numMutations = od.SelectionSet.Selections.Count;
                            userContext.MultiMutationData = new MultiMutationData(0, numMutations);
                            break;
                        case OperationType.Subscription:
                            throw new System.Exception("Not supported");
                    }
                }
            }

            return userContext;
        }
    }

    public class MultiMutationData
    {
        public MultiMutationData(int mutationIndex, int numMutations)
        {
            MutationIndex = mutationIndex;
            NumMutations = numMutations;
        }

        public void IncrementOperationIndex()
        {
            MutationIndex++;
            if (MutationIndex >= NumMutations)
                throw new System.Exception("Exceeded allowed mutation index range");
        }

        public bool IsLastMutation => MutationIndex + 1 == NumMutations;

        public int MutationIndex { get; private set; }
        public int NumMutations { get; private set; }
    }
}
