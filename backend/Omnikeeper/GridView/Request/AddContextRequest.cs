﻿using Omnikeeper.GridView.Entity;

namespace Omnikeeper.GridView.Request
{
    public class AddContextRequest
    {
        public string Name { get; set; }
        public string SpeakingName { get; set; }
        public string Description { get; set; }
        public GridViewConfiguration Configuration { get; set; }

        public AddContextRequest(string Name, string SpeakingName, string Description, GridViewConfiguration Configuration)
        {
            this.Name = Name;
            this.SpeakingName = SpeakingName;
            this.Description = Description;
            this.Configuration = Configuration;
        }
    }
}
