using Omnikeeper.GridView.Model;

namespace Omnikeeper.GridView.Request
{
    public class AddContextRequest
    {
        public string Name { get; set; }
        public GridViewConfiguration Configuration { get; set; }
    }
}
