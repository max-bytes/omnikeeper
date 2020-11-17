﻿using NUnit.Framework;
using System;
using Omnikeeper.Base.Entity.Config;

namespace Tasks.Tools
{
    [Explicit]
    class BuildBaseConfiguration
    {
        [Test]
        public void Build()
        {
            var config = new BaseConfigurationV1()
            {
                ArchiveChangesetThreshold = BaseConfigurationV1.InfiniteArchiveChangesetThreshold//TimeSpan.FromDays(90)
            };
            var json = BaseConfigurationV1.Serializer.SerializeToString(config);

            Console.WriteLine(json);
            // {"$type":"Omnikeeper.Base.Entity.Config.ApplicationConfiguration, Omnikeeper.Base","ArchiveChangesetThreshold":"90.00:00:00"}
            // {"$type":"Omnikeeper.Base.Entity.Config.BaseConfiguration, Omnikeeper.Base","ArchiveChangesetThreshold":"10675199.02:48:05.4775807"}
        }
    }
}