using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils.ModelContext
{
    public interface IModelContextBuilder
    {
        IModelContext BuildDeferred();
        IModelContext BuildDeferred(IsolationLevel isolationLevel);
        IModelContext BuildImmediate();
    }

    public interface IModelContext : IDbTransaction
    {
        NpgsqlTransaction? DBTransaction { get; }
        NpgsqlConnection DBConnection { get; }
    }
}
