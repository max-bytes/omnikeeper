using Flurl;
using Flurl.Http;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OKPluginOIASharepoint;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static OKPluginOIASharepoint.Config;
using static OKPluginOIASharepoint.OnlineInboundAdapter;

namespace OKPluginOIASharepoint
{
    public class OnlineInboundAdapter : IOnlineInboundAdapter
    {
        public class Builder : IOnlineInboundAdapterBuilder
        {
            public string Name => StaticName;
            public static string StaticName => "Sharepoint";

            public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IExternalIDMapper externalIDMapper, IExternalIDMapPersister persister, ILoggerFactory loggerFactory)
            {
                var cconfig = config as Config;
                var scopedExternalIDMapper = externalIDMapper.RegisterScoped(new ScopedExternalIDMapper(cconfig.mapperScope, persister));
                return new OnlineInboundAdapter(cconfig, scopedExternalIDMapper, loggerFactory.CreateLogger<OnlineInboundAdapter>());
            }
        }


        private readonly Config config;
        private readonly SharepointClient client;
        private readonly ExternalIDManager externalIDManager;
        private readonly ScopedExternalIDMapper scopedExternalIDMapper;

        private readonly IDictionary<Guid, CachedListConfig> cachedListConfigs;

        public OnlineInboundAdapter(Config config, ScopedExternalIDMapper scopedExternalIDMapper, ILogger logger)
        {
            this.config = config;
            cachedListConfigs = config.listConfigs.ToDictionary(lc => lc.listID, lc => new CachedListConfig(lc));

            this.scopedExternalIDMapper = scopedExternalIDMapper;

            client = new SharepointClient(config.siteDomain, config.site, new AccessTokenGetter(config));
            externalIDManager = new ExternalIDManager(cachedListConfigs, client, scopedExternalIDMapper, config.preferredIDMapUpdateRate, logger);
        }

        public IExternalIDManager GetExternalIDManager() => externalIDManager;

        public ILayerAccessProxy CreateLayerAccessProxy(Layer layer) => new LayerAccessProxy(cachedListConfigs, client, scopedExternalIDMapper, layer);
    }

    public class AccessTokenGetter
    {
        private readonly Config config;

        public AccessTokenGetter(Config config)
        {
            this.config = config;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var resource = "00000003-0000-0ff1-ce00-000000000000"; // constant, see https://docs.microsoft.com/en-us/sharepoint/dev/sp-add-ins/authorization-code-oauth-flow-for-sharepoint-add-ins#step-6-the-add-in-uses-the-authorization-code-to-request-an-access-token-from-acs-which-validates-the-request-invalidates-the-authorization-code-and-then-sends-access-and-refresh-tokens-to-the-add-in
            var tokenURL = $"https://accounts.accesscontrol.windows.net/{config.tenantID}/tokens/OAuth/2";
            var result = await tokenURL
                .WithHeader("Accept", "application/json")
                .PostUrlEncodedAsync(new List<KeyValuePair<string, string>>
                {
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                            new KeyValuePair<string, string>("client_secret", config.clientSecret),
                            new KeyValuePair<string, string>("client_id", $"{config.clientID}@{config.tenantID}"),
                            new KeyValuePair<string, string>("resource", $"{resource}/{config.siteDomain}@{config.tenantID}")
                })
                .ReceiveJson();

            string accessToken = result.access_token.ToString();

            return accessToken;
        }

        public string GetAccessToken() => GetAccessTokenAsync().GetAwaiter().GetResult();

    }

}
