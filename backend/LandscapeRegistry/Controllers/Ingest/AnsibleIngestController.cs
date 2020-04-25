using GraphQL;
using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO.Ingest;
using Landscape.Base.Model;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AnsibleIngestController : ControllerBase
    {
        private readonly IngestDataService ingestDataService;
        private readonly ILayerModel layerModel;
        private readonly ILogger<AnsibleIngestController> logger;
        private readonly ICurrentUserService currentUserService;

        public AnsibleIngestController(IngestDataService ingestDataService, ILayerModel layerModel, ICurrentUserService currentUserService, ILogger<AnsibleIngestController> logger)
        {
            this.ingestDataService = ingestDataService;
            this.layerModel = layerModel;
            this.logger = logger;
            this.currentUserService = currentUserService;
        }

        [HttpPost("IngestAnsibleInventoryScan")]
        public async Task<ActionResult> IngestAnsibleInventoryScan([FromQuery, Required]long writeLayerID, [FromQuery, Required]long[] searchLayerIDs, [FromBody, Required]AnsibleInventoryScanDTO data)
        {
            var searchLayers = new LayerSet(searchLayerIDs);
            var writeLayer = await layerModel.GetLayer(writeLayerID, null);
            var user = await currentUserService.GetCurrentUser(null);

            var cis = new Dictionary<Guid, CICandidate>();
            var relations = new List<RelationCandidate>();
            
            // ingest setup facts
            foreach(var kv in data.SetupFacts) {
                var host = kv.Key; // TODO: use?
                var facts = kv.Value["ansible_facts"];

                var tempCIID = Guid.NewGuid();
                var fqdn = facts["ansible_fqdn"].Value<string>();
                var ciName = fqdn;

                var attributeFragments = new List<BulkCICandidateAttributeData.Fragment>()
                {
                    JValue2TextAttribute(facts, "ansible_architecture", "cpu_architecture"),
                    JValue2JSONAttribute(facts, "ansible_cmdline", "ansible.inventory.cmdline"),
                    JValuePath2TextAttribute(facts, "ansible_date_time.iso8601", "ansible.inventory.last_scan_time"), // TODO: consider isodate value type?
                    JValue2JSONAttribute(facts, "ansible_default_ipv4", "default_ipv4"),
                    // jq '.ansible_facts.ansible_lvm' setup_facts.json ? TODO
                    JValue2TextAttribute(facts, "ansible_hostname", "hostname"),
                    JValue2TextAttribute(facts, "ansible_os_family", "os_family"),
                    JValue2TextAttribute(facts, "ansible_distribution", "distribution"),
                    JValue2TextAttribute(facts, "ansible_distribution_file_variety", "distribution_file_variety"),
                    JValue2TextAttribute(facts, "ansible_distribution_major_version", "distribution_major_version"),
                    JValue2TextAttribute(facts, "ansible_distribution_release", "distribution_release"),
                    JValue2TextAttribute(facts, "ansible_distribution_version", "distribution_version"),
                    JValue2IntegerAttribute(facts, "ansible_processor_vcpus", "processor_vcpus"),
                    JValue2IntegerAttribute(facts, "ansible_processor_cores", "processor_cores"),
                    JValue2IntegerAttribute(facts, "ansible_processor_count", "processor_count"),
                    JValue2TextAttribute(facts, "ansible_kernel", "kernel"),
                    JValue2IntegerAttribute(facts, "ansible_memtotal_mb", "memtotal_mb"),
                    JValue2TextArrayAttribute(facts, "ansible_interfaces", "interfaces"),
                    //#  jq '.ansible_facts.ansible_eth0' setup_facts.json # foreach in ansible_interfaces // TODO
                    JValue2JSONAttribute(facts, "ansible_dns", "dns"),
                    String2Attribute("__name", ciName),
                    String2Attribute("fqdn", fqdn)
    //#  jq '.results' yum_installed.json
    //#  jq '.results' yum_repos.json
    //#  jq '.results' yum_updates.json
                };
                var attributes = BulkCICandidateAttributeData.Build(attributeFragments);
                var ciCandidate = CICandidate.Build(CIIdentificationMethodByData.Build(new string[] { "fqdn" }), attributes);
                cis.Add(tempCIID, ciCandidate);

                // ansible mounts
                foreach (var mount in facts["ansible_mounts"])
                {
                    var tempMountCIID = Guid.NewGuid();
                    var mountValue = mount["mount"].Value<string>();
                    var ciNameMount = $"{fqdn}:{mountValue}";
                    var attributeFragmentsMount = new List<BulkCICandidateAttributeData.Fragment>
                {
                    JValue2IntegerAttribute(mount, "block_available"),
                    JValue2IntegerAttribute(mount, "block_size"),
                    JValue2IntegerAttribute(mount, "block_total"),
                    JValue2IntegerAttribute(mount, "block_used"),
                    JValue2TextAttribute(mount, "device"),
                    JValue2TextAttribute(mount, "fstype"),
                    JValue2IntegerAttribute(mount, "inode_available"),
                    JValue2IntegerAttribute(mount, "inode_total"),
                    JValue2IntegerAttribute(mount, "inode_used"),
                    String2Attribute("mount", mountValue),
                    JValue2TextAttribute(mount, "options"),
                    JValue2IntegerAttribute(mount, "size_available"),
                    JValue2IntegerAttribute(mount, "size_total"),
                    JValue2TextAttribute(mount, "uuid"),
                    String2Attribute("__name", ciNameMount)
                };
                    cis.Add(tempMountCIID, CICandidate.Build(
                        // TODO: ansible mounts have an uuid, find out what that is and if they can be used for identification
                        CIIdentificationMethodByData.Build(new string[] { "device", "mount", "__name" }), // TODO: do not use __name, rather maybe use its relation to the host for identification
                        BulkCICandidateAttributeData.Build(attributeFragmentsMount)));

                    relations.Add(RelationCandidate.Build(
                        CIIdentificationMethodByTemporaryCIID.Build(tempCIID),
                        CIIdentificationMethodByTemporaryCIID.Build(tempMountCIID),
                        "has_mounted_device"));
                }

                // ansible interfaces
                foreach (var interfaceName in facts["ansible_interfaces"].Values<string>())
                {
                    var jsonTokenName = $"ansible_{interfaceName.Replace('-', '_')}"; // TODO: ansible seems to convert - to _ for some reason... find out what else!
                    var @interface = facts[jsonTokenName];
                    var tempCIIDInterface = Guid.NewGuid();
                    var ciNameInterface = $"Network Interface {interfaceName}@{fqdn}";
                    var attributeFragmentsInterface = new List<BulkCICandidateAttributeData.Fragment>
                    {
                        JValue2TextAttribute(@interface, "device"),
                        // TODO
                        String2Attribute("__name", ciNameInterface)
                    };
                    cis.Add(tempCIIDInterface, CICandidate.Build(
                        CIIdentificationMethodByData.Build(new string[] { "__name" }), // TODO: do not use __name, rather maybe use its relation to the host for identification
                        BulkCICandidateAttributeData.Build(attributeFragmentsInterface)));

                    relations.Add(RelationCandidate.Build(
                        CIIdentificationMethodByTemporaryCIID.Build(tempCIID),
                        CIIdentificationMethodByTemporaryCIID.Build(tempCIIDInterface),
                        "has_network_interface"));
                }
            }

            var ingestData = IngestData.Build(cis, relations);
            await ingestDataService.Ingest(ingestData, writeLayer, searchLayers, user, logger);

            return Ok();
        }

        private BulkCICandidateAttributeData.Fragment String2Attribute(string name, string value) =>
            BulkCICandidateAttributeData.Fragment.Build(name, AttributeValueTextScalar.Build(value));
        private BulkCICandidateAttributeData.Fragment String2IntegerAttribute(string name, long value) =>
            BulkCICandidateAttributeData.Fragment.Build(name, AttributeValueIntegerScalar.Build(value));

        private BulkCICandidateAttributeData.Fragment JValue2TextAttribute(JToken o, string jsonName, string attributeName = null) =>
            BulkCICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeValueTextScalar.Build(o[jsonName].Value<string>()));
        private BulkCICandidateAttributeData.Fragment JValue2IntegerAttribute(JToken o, string name, string attributeName = null) =>
            BulkCICandidateAttributeData.Fragment.Build(attributeName ?? name, AttributeValueIntegerScalar.Build(o[name].Value<long>()));
        private BulkCICandidateAttributeData.Fragment JValue2JSONAttribute(JToken o, string jsonName, string attributeName = null) =>
            BulkCICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeValueJSONScalar.Build(o[jsonName] as JObject));
        private BulkCICandidateAttributeData.Fragment JValuePath2TextAttribute(JToken o, string jsonPath, string attributeName)
        {
            var jo = o.SelectToken(jsonPath);
            return BulkCICandidateAttributeData.Fragment.Build(attributeName, AttributeValueTextScalar.Build(jo.Value<string>()));
        }
        private BulkCICandidateAttributeData.Fragment JValue2TextArrayAttribute(JToken o, string jsonName, string attributeName = null)
        {
            return BulkCICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeValueTextArray.Build(o[jsonName].Values<string>().ToArray()));
        }
    }
}
