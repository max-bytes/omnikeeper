namespace Omnikeeper.Base.CLB
{
    public class CLBContext
    {
        public readonly IComputeLayerBrain Brain;

        public CLBContext(IComputeLayerBrain brain)
        {
            this.Brain = brain;
        }
    }
}
