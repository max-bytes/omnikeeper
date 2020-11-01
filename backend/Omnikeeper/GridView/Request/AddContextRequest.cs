
using Omnikeeper.Base.Entity.GridView;

namespace Omnikeeper.GridView.Request
{
    public class AddContextRequest
    {
        public string Name { get; set; }
        public GridViewConfiguration Configuration { get; set; }
    }
}
