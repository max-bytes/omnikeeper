using Omnikeeper.GridView.Entity;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetContextsResponse
    {
        public List<Context> Contexts { get; set; }

        public GetContextsResponse(List<Context> Contexts)
        {
            this.Contexts = Contexts;
        }
    }
}
