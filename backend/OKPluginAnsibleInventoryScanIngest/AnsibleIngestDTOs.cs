using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Omnikeeper.Controllers.Ingest
{
    public class AnsibleInventoryScanDTO
    {
        [JsonConstructor]
        public AnsibleInventoryScanDTO(IDictionary<string, string> setupFacts, IDictionary<string, string> yumInstalled, IDictionary<string, string> yumRepos, IDictionary<string, string> yumUpdates)
        {
            SetupFacts = setupFacts;
            YumInstalled = yumInstalled;
            YumRepos = yumRepos;
            YumUpdates = yumUpdates;
        }

        [Required]
        public IDictionary<string, string> SetupFacts { get; private set; }

        [Required]
        public IDictionary<string, string> YumInstalled { get; private set; }

        [Required]
        public IDictionary<string, string> YumRepos { get; private set; }

        [Required]
        public IDictionary<string, string> YumUpdates { get; private set; }
    }
}
