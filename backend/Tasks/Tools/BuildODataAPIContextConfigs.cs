using NUnit.Framework;
using Omnikeeper.Base.Entity;
using System;

namespace Tasks.Tools
{
    [Explicit]
    class BuildODataAPIContextConfigs
    {
        [Test]
        public void Build()
        {
            var config = new ODataAPIContext.ConfigV3(7, new long[] { 1, 2, 3, 4, 5, 6, 7 });
            var json = ODataAPIContext.ConfigSerializer.SerializeToString(config);

            Console.WriteLine(json); // {"$type":"Omnikeeper.Base.Entity.ODataAPIContext+ConfigV3, Omnikeeper.Base","WriteLayerID":7,"ReadLayerset":[1,2,3,4,5,6,7]}
        }
    }
}
