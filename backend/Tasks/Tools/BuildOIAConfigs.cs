using Omnikeeper.Base.Inbound;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks.Tools
{
    [Explicit]
    class BuildOIAConfigs
    {
        [Test]
        public void BuildOmnikeeperOIAConfig()
        {
            var config = new OKPluginOIAOmnikeeper.OnlineInboundAdapter.Config(
                "https://mhx.registry-test.mhx.at/backend/api",
                "https://auth-test.mhx.at/",
                "mhx",
                "landscape-registry-api",
                "9c98eb5f-22f0-47d1-8123-810d7d96104f",
                new string[] { "CMDB" },
                new TimeSpan(0, 1, 0),
                "omnikeeper_mhx"
                );
            var json = IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(config);

            Console.WriteLine(json);
        }

        [Test]
        public void BuildSharepointOIAConfig()
        {
            var ExternalSharepointTenantID = new Guid("98061435-3c72-44d1-b37a-057e21f42801");
            var ExternalSharepointClientID = new Guid("3d6e9642-5430-438c-b435-34d35b323b3a");
            var ExternalSharepointClientSecret = "/w5tskeWck6EV2sx6Mue1tkL+dSw42VdMHlPS5plohw=";
            var ExternalSharepointListID = new Guid("37800a8f-0107-445b-b70b-c783ba5a5ce3");

            var config = new OKPluginOIASharepoint.Config(ExternalSharepointTenantID, "mhxconsulting.sharepoint.com", "play2", ExternalSharepointClientID,
                ExternalSharepointClientSecret, true, new TimeSpan(0, 1, 0), "sharepoint",
                new OKPluginOIASharepoint.Config.ListConfig[] { new OKPluginOIASharepoint.Config.ListConfig(ExternalSharepointListID,
                    new OKPluginOIASharepoint.Config.ListColumnConfig[] {
                        new OKPluginOIASharepoint.Config.ListColumnConfig("MobilePhone", "user.mobile_phone"),
                        new OKPluginOIASharepoint.Config.ListColumnConfig("Company", "user.company"),
                        new OKPluginOIASharepoint.Config.ListColumnConfig("GivenName", "user.first_name"),
                        new OKPluginOIASharepoint.Config.ListColumnConfig("Surname", "user.last_name")
                    }, new string[] { "user.first_name", "user.last_name" }, new long[] { 1L, 2L })
                });
            var json = IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(config);

            Console.WriteLine(json);
        }
    }
}
