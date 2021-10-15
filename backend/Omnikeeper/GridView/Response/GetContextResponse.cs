using Omnikeeper.GridView.Entity;

namespace Omnikeeper.GridView.Response
{
    public class GetContextResponse
    {
        public GridViewContext Context { get; set; }

        public GetContextResponse(GridViewContext Context)
        {
            this.Context = Context;
        }

        private GetContextResponse() { }
    }
}
