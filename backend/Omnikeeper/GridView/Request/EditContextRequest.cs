using Omnikeeper.GridView.Entity;

namespace Omnikeeper.GridView.Request
{
    public class EditContextRequest
    {
        public string SpeakingName { get; set; }
        public string Description { get; set; }
        public GridViewConfiguration Configuration { get; set; }

        public EditContextRequest(string SpeakingName, string Description, GridViewConfiguration Configuration)
        {
            this.SpeakingName = SpeakingName;
            this.Description = Description;
            this.Configuration = Configuration;
        }
    }
}
