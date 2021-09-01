using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace OKPluginValidation.Validation
{
    public interface IValidationEngine
    {
        Task<bool> Run(ILogger logger);
    }
}
