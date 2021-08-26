using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Validation
{
    public interface IValidationEngine
    {
        Task<bool> Run(ILogger logger);
    }
}
