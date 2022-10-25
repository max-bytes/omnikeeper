using Npgsql;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils.ModelContext
{
    public class ModelContextBuilder : IModelContextBuilder//, IDisposable
    {
        private readonly NpgsqlConnection npgsqlConnection;

        public ModelContextBuilder(NpgsqlConnection npgsqlConnection)
        {
            this.npgsqlConnection = npgsqlConnection;
        }

        public IModelContext BuildDeferred()
        {
            var npgsqlTransaction = npgsqlConnection.BeginTransaction();
            return new ModelContextDeferredMode(npgsqlTransaction);
        }
        public IModelContext BuildDeferred(IsolationLevel isolationLevel)
        {
            var npgsqlTransaction = npgsqlConnection.BeginTransaction(isolationLevel);
            return new ModelContextDeferredMode(npgsqlTransaction);
        }

        public IModelContext BuildImmediate()
        {
            return new ModelContextImmediateMode(npgsqlConnection);
        }
    }

    public class ModelContextImmediateMode : IModelContext
    {
        private readonly NpgsqlConnection conn;
        private readonly SemaphoreSlim sem;

        public ModelContextImmediateMode(NpgsqlConnection conn)
        {
            this.conn = conn;
            sem = new SemaphoreSlim(1, 1);
        }

        public IDbConnection Connection => conn;
        public IsolationLevel IsolationLevel => default;
        public NpgsqlTransaction? DBTransaction { get; } = null;
        public NpgsqlConnection DBConnection => conn;

        public void Commit()
        {
            // NO-OP
        }

        public void Dispose()
        {
        }

        public void Rollback()
        {
            // NO-OP
        }

        public async Task<WaitToken> WaitAsync()
        {
            await sem.WaitAsync();
            return new WaitToken(() => sem.Release());
        }
    }


    public class ModelContextDeferredMode : IModelContext
    {
        private readonly SemaphoreSlim sem;

        public ModelContextDeferredMode(NpgsqlTransaction dbTransaction)
        {
            DBTransaction = dbTransaction;
            sem = new SemaphoreSlim(1, 1);
        }

        public IDbConnection? Connection => DBTransaction.Connection;
        public IsolationLevel IsolationLevel => DBTransaction.IsolationLevel;
        public NpgsqlTransaction DBTransaction { get; }
        public NpgsqlConnection DBConnection => DBTransaction.Connection!;

        public void Commit()
        {
            // TODO: need a lock here?
            DBTransaction.Commit();
        }

        public void Dispose()
        {
            DBTransaction.Dispose();
        }

        public void Rollback()
        {
            DBTransaction.Rollback();
        }


        public async Task<WaitToken> WaitAsync()
        {
            await sem.WaitAsync();
            return new WaitToken(() => sem.Release());
        }
    }
}
