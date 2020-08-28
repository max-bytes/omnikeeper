using Landscape.Base.Entity;
using LandscapeRegistry.Controllers.OData;
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
    class BuildODataAPIContextConfigs
    {
        [Test]
        public void Build()
        {
            var config = new ODataAPIContext.ConfigV2()
            {
                WriteLayerName = "CMDB",
                ReadLayersetNames = new string[] { "CMDB" }
            };
            var json = ODataAPIContext.SerializeConfigToString(config);

            Console.WriteLine(json);
        }
    }
}
