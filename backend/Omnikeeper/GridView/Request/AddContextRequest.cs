using Omnikeeper.GridView.Entity;

namespace Omnikeeper.GridView.Request
{
    public class AddContextRequest
    {
        public string ID { get; set; }
        public string SpeakingName { get; set; }
        public string Description { get; set; }
        public GridViewConfiguration Configuration { get; set; }

        public AddContextRequest(string id, string SpeakingName, string Description, GridViewConfiguration Configuration)
        {
            this.ID = id;
            this.SpeakingName = SpeakingName;
            this.Description = Description;
            this.Configuration = Configuration;
        }
    }
}
