using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Omnikeeper.Base.Utils.ModelContext
{
    public class ModelContextBuilder : IModelContextBuilder
    {
        private readonly NpgsqlConnection npgsqlConnection;
        private readonly ILogger<IModelContext> logger;

        public ModelContextBuilder(NpgsqlConnection npgsqlConnection, ILogger<IModelContext> logger)
        {
            this.npgsqlConnection = npgsqlConnection;
            this.logger = logger;
        }

        public IModelContext BuildDeferred()
        {
            var npgsqlTransaction = npgsqlConnection.BeginTransaction();
            return new ModelContextDeferredMode(npgsqlTransaction, logger);
        }
        public IModelContext BuildDeferred(IsolationLevel isolationLevel)
        {
            var npgsqlTransaction = npgsqlConnection.BeginTransaction(isolationLevel);
            return new ModelContextDeferredMode(npgsqlTransaction, logger);
        }

        public IModelContext BuildImmediate()
        {
            return new ModelContextImmediateMode(npgsqlConnection, logger);
        }
    }

    public class ModelContextImmediateMode : IModelContext
    {
        private readonly NpgsqlConnection conn;
        private readonly ILogger<IModelContext> logger;

        public ModelContextImmediateMode(NpgsqlConnection conn, ILogger<IModelContext> logger)
        {
            this.conn = conn;
            this.logger = logger;
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
    }


    public class ModelContextDeferredMode : IModelContext
    {
        private readonly ILogger<IModelContext> logger;

        public ModelContextDeferredMode(NpgsqlTransaction dbTransaction, ILogger<IModelContext> logger)
        {
            DBTransaction = dbTransaction;
            this.logger = logger;
        }

        public IDbConnection? Connection => DBTransaction.Connection;
        public IsolationLevel IsolationLevel => DBTransaction.IsolationLevel;
        public NpgsqlTransaction DBTransaction { get; }
        public NpgsqlConnection DBConnection => DBTransaction.Connection!;

        public void Commit()
        {
            // TODO: need a distributed lock here
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
    }
}
