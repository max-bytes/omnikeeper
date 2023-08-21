using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class DynamicAuthSchemeService
    {
        private readonly IAuthenticationSchemeProvider schemeProvider;
        //private readonly IOptionsMonitorCache<O> optionsCache;

        public DynamicAuthSchemeService(IAuthenticationSchemeProvider schemeProvider)//, IOptionsMonitorCache<SimpleOptions> optionsCache)
        {
            this.schemeProvider = schemeProvider;
            //this.optionsCache = optionsCache;
        }

        public async Task<bool> TryAdd(string scheme, System.Type handlerType)
        {
            if (await schemeProvider.GetSchemeAsync(scheme) == null)
            {
                schemeProvider.AddScheme(new AuthenticationScheme(scheme, scheme, handlerType));
                return true;
            } 
            else
            {
                return false;
            }
            //else
            //{
            //    _optionsCache.TryRemove(scheme);
            //}
            //_optionsCache.TryAdd(scheme, new SimpleOptions { DisplayMessage = optionsMessage });
        }
    }
}
