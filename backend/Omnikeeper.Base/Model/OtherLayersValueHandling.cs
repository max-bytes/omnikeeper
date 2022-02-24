using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface IOtherLayersValueHandling
    {
    }

    public class OtherLayersValueHandlingForceWrite : IOtherLayersValueHandling
    {
        private OtherLayersValueHandlingForceWrite() { }

        public static OtherLayersValueHandlingForceWrite Instance = new OtherLayersValueHandlingForceWrite();
    }

    public class OtherLayersValueHandlingTakeIntoAccount : IOtherLayersValueHandling
    {
        public readonly string[] ReadLayersWithoutWriteLayer;

        private OtherLayersValueHandlingTakeIntoAccount(string[] readLayersWithoutWriteLayer)
        {
            ReadLayersWithoutWriteLayer = readLayersWithoutWriteLayer;
        }

        public static IOtherLayersValueHandling Build(LayerSet readLayerSet, string writeLayerID)
        {
            var indexWriteLayerID = readLayerSet.IndexOf(writeLayerID);
            if (indexWriteLayerID == -1)
                throw new Exception("Cannot create other-layers-value-handling object when write layer ID is not contained within read layerset");
            var readLayersWithoutWriteLayer = readLayerSet.Take(indexWriteLayerID).Concat(readLayerSet.Skip(indexWriteLayerID + 1)).ToArray();
            if (readLayersWithoutWriteLayer.Length == 0)
                return OtherLayersValueHandlingForceWrite.Instance;
            return new OtherLayersValueHandlingTakeIntoAccount(readLayersWithoutWriteLayer);
        }
    }
}
