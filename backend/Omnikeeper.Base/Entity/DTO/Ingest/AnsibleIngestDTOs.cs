using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO.Ingest
{
    public class AnsibleInventoryScanDTO
    {
        [Required]
        public IDictionary<string, JObject> SetupFacts { get; set; }

        [Required]
        public IDictionary<string, JObject> YumInstalled { get; set; }

        [Required]
        public IDictionary<string, JObject> YumRepos { get; set; }

        [Required]
        public IDictionary<string, JObject> YumUpdates { get; set; }
    }
}
