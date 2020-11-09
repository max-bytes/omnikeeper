using Npgsql;
using Omnikeeper.Base.Entity;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITemplatesProvider
    {
        public Task<Templates> GetTemplates(NpgsqlTransaction trans);
    }
}
