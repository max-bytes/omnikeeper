using Landscape.Base.Entity;
using LandscapeRegistry.Controllers.OData;
using Newtonsoft.Json;
using NUnit.Framework;
using OnlineInboundAdapterOmnikeeper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks.Tools
{
    [Explicit]
    class BuildODataAPIContextConfigs
    {
        [Test]
        public void Build()
        {
            var config = new ODataAPIContext.ConfigV3()
            {
                WriteLayerID = 7,
                ReadLayerset = new long[] { 1,2,3,4,5,6,7 }
            };
            var json = ODataAPIContext.ConfigSerializer.SerializeToString(config);

            Console.WriteLine(json);
        }
    }
}
