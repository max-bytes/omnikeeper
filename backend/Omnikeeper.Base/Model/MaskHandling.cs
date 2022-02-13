using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface IMaskHandlingForRetrieval { }

    public class MaskHandlingForRetrievalApplyMasks : IMaskHandlingForRetrieval
    {
        private MaskHandlingForRetrievalApplyMasks() { }

        public static MaskHandlingForRetrievalApplyMasks Instance = new MaskHandlingForRetrievalApplyMasks();
    }
    public class MaskHandlingForRetrievalGetMasks : IMaskHandlingForRetrieval
    {
        private MaskHandlingForRetrievalGetMasks() { }

        public static MaskHandlingForRetrievalGetMasks Instance = new MaskHandlingForRetrievalGetMasks();
    }

public interface IMaskHandlingForRemoval { }

    /// <summary>
    /// detects if the layers "below" contain the attribute too
    /// if that is the case:
    ///  -> instead of removing the attribute, inserts an attribute mask instead
    /// if the layers "below" do NOT contain the attribute:
    ///  -> remove the attribute in the regular fashion; if the attribute-to-remove is already a mask, it is removed too
    /// </summary>
    public class MaskHandlingForRemovalApplyMaskIfNecessary : IMaskHandlingForRemoval
    {
        public readonly string[] ReadLayersBelowWriteLayer;

        private MaskHandlingForRemovalApplyMaskIfNecessary(string[] readLayersBelowWriteLayer)
        {
            ReadLayersBelowWriteLayer = readLayersBelowWriteLayer;
        }

        public static IMaskHandlingForRemoval Build(string[] readLayersBelowWriteLayer)
        {
            if (readLayersBelowWriteLayer.Length == 0)
                return MaskHandlingForRemovalApplyNoMask.Instance;
            return new MaskHandlingForRemovalApplyMaskIfNecessary(readLayersBelowWriteLayer);
        }

        public static IMaskHandlingForRemoval Build(LayerSet readLayerSet, string writeLayerID)
        {
            var indexWriteLayerID = readLayerSet.IndexOf(writeLayerID);
            if (indexWriteLayerID == -1)
                throw new System.Exception("Cannot create mask handling object when write layer ID is not contained within read layerset");
            var readLayersBelowWriteLayer = readLayerSet.Skip(indexWriteLayerID + 1).ToArray();
            if (readLayersBelowWriteLayer.Length == 0)
                return MaskHandlingForRemovalApplyNoMask.Instance;
            return new MaskHandlingForRemovalApplyMaskIfNecessary(readLayersBelowWriteLayer);
        }
    }

    /// <summary>
    /// ignores any masking, simply removes the attribute if its present
    /// if the attribute-to-remove is a mask, it removes it too
    /// </summary>
    public class MaskHandlingForRemovalApplyNoMask : IMaskHandlingForRemoval
    {
        private MaskHandlingForRemovalApplyNoMask() { }

        public static MaskHandlingForRemovalApplyNoMask Instance = new MaskHandlingForRemovalApplyNoMask();
    }
}
