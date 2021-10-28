using Omnikeeper.Base.Entity;

namespace Omnikeeper.Base.CLB
{
    public class CLBContext
    {
        public readonly UserInDatabase UserInDatabase;

        public CLBContext(UserInDatabase userInDatabase)
        {
            this.UserInDatabase = userInDatabase;
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
