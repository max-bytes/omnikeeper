using NUnit.Framework;
using Omnikeeper.Base.Entity.Config;
using System;

namespace Tasks.Tools
{
    [Explicit]
    class BuildBaseConfiguration
    {
        [Test]
        public void Build()
        {
            var config = new BaseConfigurationV2(BaseConfigurationV2.InfiniteArchiveDataThreshold, "0 0 0 0 0", "0 0 0 0 0", "0 0 0 0 0", "0 0 0 0 0");
            var json = BaseConfigurationV2.Serializer.SerializeToString(config);

            Console.WriteLine(json);
            // {"$type":"Omnikeeper.Base.Entity.Config.ApplicationConfiguration, Omnikeeper.Base","ArchiveChangesetThreshold":"90.00:00:00"}
            // {"$type":"Omnikeeper.Base.Entity.Config.BaseConfiguration, Omnikeeper.Base","ArchiveChangesetThreshold":"10675199.02:48:05.4775807"}
        }
    }
}
