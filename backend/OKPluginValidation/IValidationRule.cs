using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginValidation.Validation
{
    public interface IValidationRule
    {
        string Name { get; }

        Task<IEnumerable<ValidationIssue>> PerformValidation(JObject config, IModelContext trans, TimeThreshold atTime);
    }
}
