using Omnikeeper.GridView.Entity;

namespace Omnikeeper.GridView.Response
{
    public class GetContextResponse
    {
        public FullContext Context { get; set; }

        public GetContextResponse(FullContext Context)
        {
            this.Context = Context;
        }
    }
}
