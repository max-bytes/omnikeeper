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
            var config = new BaseConfigurationV1(BaseConfigurationV1.InfiniteArchiveChangesetThreshold, "0 0 0 0 0", "0 0 0 0 0", "0 0 0 0 0", "0 0 0 0 0", new string[] { "1" },  "1");
            var json = BaseConfigurationV1.Serializer.SerializeToString(config);

            Console.WriteLine(json);
            // {"$type":"Omnikeeper.Base.Entity.Config.ApplicationConfiguration, Omnikeeper.Base","ArchiveChangesetThreshold":"90.00:00:00"}
            // {"$type":"Omnikeeper.Base.Entity.Config.BaseConfiguration, Omnikeeper.Base","ArchiveChangesetThreshold":"10675199.02:48:05.4775807"}
        }
    }
}
