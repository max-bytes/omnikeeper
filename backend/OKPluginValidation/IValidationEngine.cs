using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace OKPluginValidation
{
    public interface IValidationEngine
    {
        Task<bool> Run(ILogger logger);
    }
}
