using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Landscape.Base.Entity.DTO.Ingest
{
    public class AnsibleInventoryScanDTO
    {
        [Required]
        public IDictionary<string, JObject> SetupFacts { get; set; }
    }
}
