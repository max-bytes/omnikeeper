using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginValidation
{
    public interface IValidationRule
    {
        string Name { get; }

        Task<IEnumerable<ValidationIssue>> PerformValidation(Validation validation, Guid validationCIID, IModelContext trans, TimeThreshold atTime);
    }
}
