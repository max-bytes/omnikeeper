using Omnikeeper.Base.Utils;

namespace Omnikeeper.Base.Model
{
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
        public readonly TimeThreshold ReadTime;

        public MaskHandlingForRemovalApplyMaskIfNecessary(string[] readLayersBelowWriteLayer, TimeThreshold readTime)
        {
            ReadLayersBelowWriteLayer = readLayersBelowWriteLayer;
            ReadTime = readTime;
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
