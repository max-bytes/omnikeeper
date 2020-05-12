using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITemplateModel
    {
        //Task<TemplateErrorsCI> CalculateTemplateErrors(Guid ciid, LayerSet layerset, ICIModel ciModel, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<TemplateErrorsCI> CalculateTemplateErrors(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
