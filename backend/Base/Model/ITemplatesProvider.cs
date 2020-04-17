using Landscape.Base.Entity;
using Npgsql;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITemplatesProvider
    {
        public Task<Templates> GetTemplates(NpgsqlTransaction trans);
    }
}
