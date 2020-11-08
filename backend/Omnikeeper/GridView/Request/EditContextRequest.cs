using Omnikeeper.Base.Entity.GridView;

namespace Omnikeeper.GridView.Request
{
    public class EditContextRequest
    {
        public string SpeakingName { get; set; }
        public string Description { get; set; }
        public GridViewConfiguration Configuration { get; set; }
    }
}
