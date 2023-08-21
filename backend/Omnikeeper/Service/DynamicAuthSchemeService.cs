using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Omnikeeper.Base.Service;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class DynamicAuthSchemeService : IDynamicAuthSchemeService
    {
        private readonly IAuthenticationSchemeProvider schemeProvider;
        private readonly IOptionsMonitorCache<AuthenticationSchemeOptions> optionsCache;

        public DynamicAuthSchemeService(IAuthenticationSchemeProvider schemeProvider, IOptionsMonitorCache<AuthenticationSchemeOptions> optionsCache)
        {
            this.schemeProvider = schemeProvider;
            this.optionsCache = optionsCache;
        }

        public async Task<bool> TryAdd(string scheme, System.Type handlerType, AuthenticationSchemeOptions? options = null)
        {
            if (await schemeProvider.GetSchemeAsync(scheme) == null)
            {
                schemeProvider.AddScheme(new AuthenticationScheme(scheme, scheme, handlerType));
                if (options != null)
                    optionsCache.TryAdd(scheme, options);

                return true;
            } 
            else
            {
                return false;
            }
        }
    }
}
