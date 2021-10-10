using Omnikeeper.GridView.Entity;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetContextsResponse
    {
        public List<FullContext> Contexts { get; set; }

        public GetContextsResponse(List<FullContext> Contexts)
        {
            this.Contexts = Contexts;
        }

        private GetContextsResponse() { }
    }
}
