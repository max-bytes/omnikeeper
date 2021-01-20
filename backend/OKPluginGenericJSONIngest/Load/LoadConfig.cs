using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginGenericJSONIngest.Load
{
    public interface ILoadConfig
    {
        long[] SearchLayerIDs { get; }
        long WriteLayerID { get; }
    }

    public class LoadConfig : ILoadConfig
    {
        public LoadConfig(long[] searchLayerIDs, long writeLayerID)
        {
            SearchLayerIDs = searchLayerIDs;
            WriteLayerID = writeLayerID;
        }

        public long[] SearchLayerIDs { get; }
        public long WriteLayerID { get; }
    }
}
