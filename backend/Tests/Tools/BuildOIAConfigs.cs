using Newtonsoft.Json;
using NUnit.Framework;
using OnlineInboundAdapterOmnikeeper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Tools
{
    [Explicit]
    //[Ignore("Only manual")]
    class BuildOIAConfigs
    {
        [Test]
        public void Build()
        {
            var config = new OnlineInboundAdapter.Config(
                "https://mhx.registry-test.mhx.at/backend/api",
                "https://auth-test.mhx.at/auth/",
                "mhx",
                "landscape-registry-api",
                "9c98eb5f-22f0-47d1-8123-810d7d96104f",
                new string[] { "CMDB" },
                new TimeSpan(0, 1, 0),
                "omnikeeper_mhx"
                );
            var json = JsonConvert.SerializeObject(config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects });

            Console.WriteLine(json);
        }
    }
}
