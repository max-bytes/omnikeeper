using System.Runtime.Serialization;

namespace Omnikeeper.GridView.Entity
{
    public class FullContext
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }
        public string SpeakingName { get; set; }
        public string Description { get; set; }
        public GridViewConfiguration Configuration { get; set; }

        public FullContext(string id, string SpeakingName, string Description, GridViewConfiguration Configuration)
        {
            this.ID = id;
            this.SpeakingName = SpeakingName;
            this.Description = Description;
            this.Configuration = Configuration;
        }

        private FullContext() { }
    }
}
