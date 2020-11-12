using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITemplateModel
    {
        //Task<TemplateErrorsCI> CalculateTemplateErrors(Guid ciid, LayerSet layerset, ICIModel ciModel, ITransaction trans, TimeThreshold atTime);
        Task<TemplateErrorsCI> CalculateTemplateErrors(MergedCI ci, IModelContext trans, TimeThreshold atTime);
    }
}
