using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO.Ingest // TODO: move into plugin
{
    public class AnsibleInventoryScanDTO
    {
        [JsonConstructor]
        public AnsibleInventoryScanDTO(IDictionary<string, JObject> setupFacts, IDictionary<string, JObject> yumInstalled, IDictionary<string, JObject> yumRepos, IDictionary<string, JObject> yumUpdates)
        {
            SetupFacts = setupFacts;
            YumInstalled = yumInstalled;
            YumRepos = yumRepos;
            YumUpdates = yumUpdates;
        }

        [Required]
        public IDictionary<string, JObject> SetupFacts { get; private set; }

        [Required]
        public IDictionary<string, JObject> YumInstalled { get; private set; }

        [Required]
        public IDictionary<string, JObject> YumRepos { get; private set; }

        [Required]
        public IDictionary<string, JObject> YumUpdates { get; private set; }
    }
}
