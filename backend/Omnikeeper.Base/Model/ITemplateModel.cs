using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Npgsql;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITemplateModel
    {
        //Task<TemplateErrorsCI> CalculateTemplateErrors(Guid ciid, LayerSet layerset, ICIModel ciModel, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<TemplateErrorsCI> CalculateTemplateErrors(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
