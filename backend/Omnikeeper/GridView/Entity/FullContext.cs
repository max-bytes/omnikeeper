namespace Omnikeeper.GridView.Entity
{
    public class FullContext
    {
        public string Name { get; set; }
        public string? SpeakingName { get; set; }
        public string? Description { get; set; }
        public GridViewConfiguration Configuration { get; set; }

        public FullContext(string Name, string? SpeakingName, string? Description, GridViewConfiguration Configuration)
        {
            this.Name = Name;
            this.SpeakingName = SpeakingName;
            this.Description = Description;
            this.Configuration = Configuration;
        }
    }
}
