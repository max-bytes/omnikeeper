using Omnikeeper.Base.Entity.GridView;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetContextsResponse
    {
        public List<Context> Contexts { get; set; }
    }

    //public class Context
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public string SpeakingName { get; set; }
    //    public string Description { get; set; }
    //}
}
