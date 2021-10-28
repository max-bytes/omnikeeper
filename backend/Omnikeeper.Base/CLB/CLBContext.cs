using Omnikeeper.Base.Entity;

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

    public class CLBContextAccessor
    {
        public void SetCLBContext(CLBContext context)
        {
            CLBContext = context;
        }
        public void ClearCLBContext()
        {
            CLBContext = null;
        }

        public CLBContext? CLBContext { get; private set; } = null;
    }
}
