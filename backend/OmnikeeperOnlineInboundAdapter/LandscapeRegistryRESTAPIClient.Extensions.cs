using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static OnlineInboundAdapterOmnikeeper.OnlineInboundAdapter;

namespace OnlineInboundAdapterOmnikeeper
{
    public partial class LandscapeRegistryRESTAPIClient
    {
        private readonly Config config;

        public LandscapeRegistryRESTAPIClient(Config config, HttpClient httpClient) : this(httpClient)
        {
            this.config = config;
        }

        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
        {
            string GetAccessToken() => GetAccessTokenAsync(config.authURL, config.realm, config.clientID, config.clientSecret).GetAwaiter().GetResult();
            var accessToken = GetAccessToken(); // TODO: this sucks, we shouldn't execute this in a constructor -> find better way
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
        {
            settings.ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy(false, false, false)
            };
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        private async Task<string> GetAccessTokenAsync(string url, string realm, string client_id, string client_secret)
        {
            var result = await url
                .AppendPathSegment($"/auth/realms/{realm}/protocol/openid-connect/token")
                .WithHeader("Accept", "application/json")
                .PostUrlEncodedAsync(new List<KeyValuePair<string, string>>
                {
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                            new KeyValuePair<string, string>("client_secret", client_secret),
                            new KeyValuePair<string, string>("client_id", client_id)
                })
                .ReceiveJson();

            string accessToken = result.access_token.ToString();

            return accessToken;
        }

    }
}
