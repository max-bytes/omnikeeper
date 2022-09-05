using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.GraphQL
{
    public class OmnikeeperUserContext : Dictionary<string, object?>, IOmnikeeperUserContext, IDisposable
    {
        public IAuthenticatedUser User { get; private set; }
        public IServiceProvider ServiceProvider { get; }

        private class ScopedContext
        {
            public TimeThreshold? timeThreshold;
            public LayerSet? layerSet;

            public IDictionary<string, ScopedContext>? subContexts;
        }

        private readonly ScopedContext rootScopedContext = new ScopedContext();

        private IEnumerable<ScopedContext> FindScopedContexts(IList<object> contextPath, int skipFirstN, ScopedContext currentScopedContext)
        {
            if (skipFirstN >= contextPath.Count)
            {
                yield return currentScopedContext;
                yield break;
            }

            if (currentScopedContext.subContexts == null)
            {
                yield return currentScopedContext;
                yield break;
            }

            var t = contextPath[skipFirstN];
            if (t is string tt)
            {
                if (currentScopedContext.subContexts.TryGetValue(tt, out var subScopedContext))
                {
                    var sub = FindScopedContexts(contextPath, skipFirstN + 1, subScopedContext);
                    foreach (var s in sub)
                        yield return s;
                }
            }
            else
            {
                var sub = FindScopedContexts(contextPath, skipFirstN + 1, currentScopedContext);
                foreach (var s in sub)
                    yield return s;
            }
            yield return currentScopedContext;
        }

        private ScopedContext FindOrCreateScopedContext(IReadOnlyList<object> contextPath, int skipFirstN, ScopedContext currentScopedContext)
        {
            if (skipFirstN >= contextPath.Count)
                return currentScopedContext;

            if (currentScopedContext.subContexts == null)
                currentScopedContext.subContexts = new Dictionary<string, ScopedContext>();

            var t = contextPath[skipFirstN];
            if (t is string tt)
            {
                if (currentScopedContext.subContexts.TryGetValue(tt, out var subScopedContext))
                    return FindOrCreateScopedContext(contextPath, skipFirstN + 1, subScopedContext);
                else
                {
                    currentScopedContext.subContexts[tt] = new ScopedContext();
                    return FindOrCreateScopedContext(contextPath, skipFirstN + 1, currentScopedContext.subContexts[tt]);
                }
            }
            else
            {
                return FindOrCreateScopedContext(contextPath, skipFirstN + 1, currentScopedContext);
            }
        }


        public OmnikeeperUserContext(IAuthenticatedUser user, IServiceProvider sp)
        {
            User = user;
            ServiceProvider = sp;
        }

        public TimeThreshold GetTimeThreshold(IEnumerable<object> contextPath)
        {
            var foundContexts = FindScopedContexts(contextPath.ToList(), 0, rootScopedContext);
            foreach(var fc in foundContexts)
            {
                if (fc.timeThreshold.HasValue)
                    return fc.timeThreshold.Value;
            }
            throw new Exception("TimeThreshold not set in current user context"); // throw exception, demand explicit setting
        }

        public LayerSet GetLayerSet(IEnumerable<object> contextPath)
        {
            var foundContexts = FindScopedContexts(contextPath.ToList(), 0, rootScopedContext);
            foreach (var fc in foundContexts)
            {
                if (fc.layerSet != null)
                    return fc.layerSet;
            }
            throw new Exception("LayerSet not set in current user context"); // throw exception, demand explicit setting
        }

        public IModelContext Transaction
        {
            get
            {
                TryGetValue("Transaction", out var t);
                if (t == null) throw new System.Exception("Expected transaction to be set");
                return (IModelContext)t;
            }
            private set
            {
                this.AddOrUpdate("Transaction", value);
            }
        }

        public IChangesetProxy ChangesetProxy
        {
            get
            {
                TryGetValue("ChangesetProxy", out var t);
                if (t == null)
                    throw new System.Exception("Expected ChangesetProxy to be set");
                return (IChangesetProxy)t;
            }
            private set
            {
                this.AddOrUpdate("ChangesetProxy", value);
            }
        }

        public OmnikeeperUserContext WithTransaction(Func<IModelContextBuilder, IModelContext> f)
        {
            if (!ContainsKey("Transaction"))
            {
                var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
                Transaction = f(modelContextBuilder);
            }
            return this;
        }

        public OmnikeeperUserContext WithTimeThreshold(TimeThreshold ts, IEnumerable<object> contextPath)
        {
            var foundContext = FindOrCreateScopedContext(contextPath.ToList(), 0, rootScopedContext);
            foundContext.timeThreshold = ts;
            return this;
        }

        public async Task<OmnikeeperUserContext> WithLayersetAsync(Func<IModelContext, Task<LayerSet>> f, IEnumerable<object> contextPath)
        {
            var foundContext = FindOrCreateScopedContext(contextPath.ToList(), 0, rootScopedContext);
            var ls = await f(Transaction);
            foundContext.layerSet = ls;
            return this;
        }

        public OmnikeeperUserContext WithLayerset(LayerSet ls, IEnumerable<object> contextPath)
        {
            var foundContext = FindOrCreateScopedContext(contextPath.ToList(), 0, rootScopedContext);
            foundContext.layerSet = ls;
            return this;
        }

        public OmnikeeperUserContext WithChangesetProxy(IChangesetModel changesetModel, TimeThreshold timeThreshold, DataOriginV1 dataOrigin)
        {
            var changesetProxy = new ChangesetProxy(User.InDatabase, timeThreshold, changesetModel, dataOrigin);
            ChangesetProxy = changesetProxy;
            return this;
        }

        public void CommitAndStartNewTransactionIfLastMutationAndNoErrors(IResolveFieldContext rfc, Func<IModelContextBuilder, IModelContext> f)
        {
            if (rfc.Document.Definitions[0] is not GraphQLParser.AST.GraphQLOperationDefinition operationDefinition)
                throw new Exception();
            if (operationDefinition.Operation != GraphQLParser.AST.OperationType.Mutation)
                throw new Exception();

            var numMutations = operationDefinition.SelectionSet.Selections.Count;
            var currentAlias = rfc.FieldAst.Alias;

            // find out if we are in the last mutation by checking the AST and comparing our current alias to the list of aliases
            bool isLastMutation;
            if (numMutations <= 1 || currentAlias == null)
            {
                isLastMutation = true;
            } else
            {
                var currentMutationName = currentAlias.Name;
                var currentMutationIndex = operationDefinition.SelectionSet.Selections.FindIndex(s => (s as GraphQLParser.AST.GraphQLField)!.Alias!.Name == currentMutationName);
                isLastMutation = currentMutationIndex == numMutations - 1;
            }

            if (isLastMutation)
            {
                if (rfc.Errors.IsEmpty())
                { // only if this and all previous mutations did not produce any errors, we actually commit
                    Transaction.Commit();
                    var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
                    Transaction = f(modelContextBuilder);
                }
            }
        }

        public void Dispose()
        {
            if (TryGetValue("Transaction", out var t))
            {
                if (t is IModelContext mc)
                    mc.Dispose();
            }
        }
    }
}
