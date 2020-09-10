using Landscape.Base.Inbound;
using Newtonsoft.Json;
using NUnit.Framework;
using OnlineInboundAdapterOmnikeeper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks.Tools
{
    [Explicit]
    class BuildOIAConfigs
    {
        [Test]
        public void Build()
        {
            var config = new OnlineInboundAdapter.Config(
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
    }
}
