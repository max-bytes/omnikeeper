namespace Omnikeeper.GridView.Entity
{
    public class Context
    {
        public string ID { get; set; }
        public string SpeakingName { get; set; }
        public string Description { get; set; }

        public Context(string id, string SpeakingName, string Description)
        {
            this.ID = id;
            this.SpeakingName = SpeakingName;
            this.Description = Description;
        }
    }
}
