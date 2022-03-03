using Npgsql;
using System.Data;

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
