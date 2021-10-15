using Omnikeeper.GridView.Entity;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetContextsResponse
    {
        public List<GridViewContext> Contexts { get; set; }

        public GetContextsResponse(List<GridViewContext> Contexts)
        {
            this.Contexts = Contexts;
        }

        private GetContextsResponse() { }
    }
}
