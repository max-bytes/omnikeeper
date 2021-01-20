namespace Omnikeeper.GridView.Entity
{
    public class Context
    {
        public string Name { get; set; }
        public string? SpeakingName { get; set; }
        public string? Description { get; set; }

        public Context(string Name, string? SpeakingName, string? Description)
        {
            this.Name = Name;
            this.SpeakingName = SpeakingName;
            this.Description = Description;
        }
    }
}
