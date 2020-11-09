using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO.Ingest;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.Ingest
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
        private readonly ILayerBasedAuthorizationService authorizationService;

        public AnsibleIngestController(IngestDataService ingestDataService, ILayerModel layerModel, ICurrentUserService currentUserService,
            ILayerBasedAuthorizationService authorizationService, ILogger<AnsibleIngestController> logger)
        {
            this.ingestDataService = ingestDataService;
            this.layerModel = layerModel;
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
        }

        [HttpPost("IngestAnsibleInventoryScan")]
        public async Task<ActionResult> IngestAnsibleInventoryScan([FromQuery, Required] long writeLayerID, [FromQuery, Required] long[] searchLayerIDs, [FromBody, Required] AnsibleInventoryScanDTO data)
        {
            try
            {
                var searchLayers = new LayerSet(searchLayerIDs);
                var writeLayer = await layerModel.GetLayer(writeLayerID, null);
                var user = await currentUserService.GetCurrentUser(null);

                // authorization
                if (!authorizationService.CanUserWriteToLayer(user, writeLayer))
                {
                    return Forbid();
                }
                // NOTE: we don't do any ci-based authorization here... its pretty hard to do because of all the temporary CIs
                // TODO: think about this!

                var cis = new Dictionary<Guid, CICandidate>();
                var relations = new List<RelationCandidate>();

                // ingest setup facts
                var baseCIs = new Dictionary<string, (Guid ciid, CICandidate ci)>();
                foreach (var kv in data.SetupFacts)
                {
                    var host = kv.Key; // TODO: use?
                    var facts = kv.Value["ansible_facts"];

                    var tempCIID = Guid.NewGuid();
                    var fqdn = facts["ansible_fqdn"].Value<string>();
                    var ciName = fqdn;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment>()
                    {
                        JValue2TextAttribute(facts, "ansible_architecture", "cpu_architecture"),
                        JValue2JSONAttribute(facts, "ansible_cmdline", "ansible.inventory.cmdline"),
                        JValuePath2TextAttribute(facts, "ansible_date_time.iso8601", "ansible.inventory.last_scan_time"), // TODO: consider isodate value type?
                        JValue2JSONAttribute(facts, "ansible_default_ipv4", "default_ipv4"),
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
                        JValue2JSONAttribute(facts, "ansible_dns", "dns"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    };
                    var attributes = CICandidateAttributeData.Build(attributeFragments);
                    var ciCandidate = CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes);
                    cis.Add(tempCIID, ciCandidate);

                    baseCIs.Add(fqdn, (tempCIID, ciCandidate));

                    // ansible mounts
                    foreach (var mount in facts["ansible_mounts"])
                    {
                        var tempMountCIID = Guid.NewGuid();
                        var mountValue = mount["mount"].Value<string>();
                        var ciNameMount = $"{fqdn}:{mountValue}";
                        var attributeFragmentsMount = new List<CICandidateAttributeData.Fragment>
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
                        String2Attribute(ICIModel.NameAttribute, ciNameMount)
                    };
                        var attributeData = CICandidateAttributeData.Build(attributeFragmentsMount);
                        cis.Add(tempMountCIID, CICandidate.Build(
                            // TODO: ansible mounts have an uuid, find out what that is and if they can be used for identification
                            CIIdentificationMethodByData.BuildFromAttributes(new string[] { "device", "mount", ICIModel.NameAttribute }, attributeData, searchLayers), // TODO: do not use CIModel.NameAttribute, rather maybe use its relation to the host for identification
                            attributeData));

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
                        var attributeFragmentsInterface = new List<CICandidateAttributeData.Fragment>
                        {
                            JValue2TextAttribute(@interface, "device"),
                            JValue2TextAttribute(@interface, "active"),
                            JValue2TextAttribute(@interface, "type"),
                            Try2(() => JValue2TextAttribute(@interface, "macaddress")),
                            // TODO
                            String2Attribute(ICIModel.NameAttribute, ciNameInterface)
                        }.Where(item => item != null);
                        var attributeData = CICandidateAttributeData.Build(attributeFragmentsInterface);
                        cis.Add(tempCIIDInterface, CICandidate.Build(
                            CIIdentificationMethodByData.BuildFromAttributes(new string[] { ICIModel.NameAttribute }, attributeData, searchLayers), // TODO: do not use CIModel.NameAttribute, rather maybe use its relation to the host for identification
                            attributeData));

                        relations.Add(RelationCandidate.Build(
                            CIIdentificationMethodByTemporaryCIID.Build(tempCIID),
                            CIIdentificationMethodByTemporaryCIID.Build(tempCIIDInterface),
                            "has_network_interface"));
                    }
                }

                // yum related data
                foreach (var kvInstalled in data.YumInstalled)
                {
                    var hostID = kvInstalled.Key;
                    var fqdn = hostID; // TODO: check if using the HostID as fqdn is ok
                    var ciName = hostID;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment>()
                    {
                        JToken2JSONAttribute(kvInstalled.Value["results"], "yum.installed"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    };
                    var attributes = CICandidateAttributeData.Build(attributeFragments);

                    if (baseCIs.TryGetValue(fqdn, out var @base))
                    { // attach to base CI
                        cis[@base.ciid] = CICandidate.BuildWithAdditionalAttributes(@base.ci, attributes);
                    }
                    else
                    { // treat as new CI
                        cis.Add(Guid.NewGuid(), CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes));
                    }
                }
                foreach (var kvRepos in data.YumRepos)
                {
                    var hostID = kvRepos.Key;
                    var fqdn = hostID; // TODO: check if using the HostID as fqdn is ok
                    var ciName = hostID;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment>()
                    {
                        JToken2JSONAttribute(kvRepos.Value["results"], "yum.repos"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    };
                    var attributes = CICandidateAttributeData.Build(attributeFragments);

                    if (baseCIs.TryGetValue(fqdn, out var @base))
                    { // attach to base CI
                        cis[@base.ciid] = CICandidate.BuildWithAdditionalAttributes(@base.ci, attributes);
                    }
                    else
                    { // treat as new CI
                        cis.Add(Guid.NewGuid(), CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes));
                    }
                }
                foreach (var kvUpdates in data.YumUpdates)
                {
                    var hostID = kvUpdates.Key;
                    var fqdn = hostID; // TODO: check if using the HostID as fqdn is ok
                    var ciName = hostID;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment>()
                    {
                        JToken2JSONAttribute(kvUpdates.Value["results"], "yum.updates"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    };
                    var attributes = CICandidateAttributeData.Build(attributeFragments);

                    if (baseCIs.TryGetValue(fqdn, out var @base))
                    { // attach to base CI
                        cis[@base.ciid] = CICandidate.BuildWithAdditionalAttributes(@base.ci, attributes);
                    }
                    else
                    { // treat as new CI
                        cis.Add(Guid.NewGuid(), CICandidate.Build(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes));
                    }
                }

                var ingestData = IngestData.Build(cis, relations);
                var (numIngestedCIs, numIngestedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, user, logger);

                logger.LogInformation($"Ansible Ingest successful; ingested {numIngestedCIs} CIs, {numIngestedRelations} relations");

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ansible Ingest failed");
                return BadRequest();
            }
        }

        private CICandidateAttributeData.Fragment Try2(Func<CICandidateAttributeData.Fragment> f)
        {
            try
            {
                return f();
            }
            catch
            {
                return null;
            }
        }

        private CICandidateAttributeData.Fragment String2Attribute(string name, string value) =>
            CICandidateAttributeData.Fragment.Build(name, AttributeScalarValueText.BuildFromString(value));
        private CICandidateAttributeData.Fragment String2IntegerAttribute(string name, long value) =>
            CICandidateAttributeData.Fragment.Build(name, AttributeScalarValueInteger.Build(value));

        private CICandidateAttributeData.Fragment JValue2TextAttribute(JToken o, string jsonName, string attributeName = null) =>
            CICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeScalarValueText.BuildFromString(o[jsonName].Value<string>()));
        private CICandidateAttributeData.Fragment JValue2IntegerAttribute(JToken o, string name, string attributeName = null) =>
            CICandidateAttributeData.Fragment.Build(attributeName ?? name, AttributeScalarValueInteger.Build(o[name].Value<long>()));
        private CICandidateAttributeData.Fragment JValue2JSONAttribute(JToken o, string jsonName, string attributeName = null) =>
            CICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeScalarValueJSON.Build(o[jsonName]));
        private CICandidateAttributeData.Fragment JValuePath2TextAttribute(JToken o, string jsonPath, string attributeName)
        {
            var jo = o.SelectToken(jsonPath);
            return CICandidateAttributeData.Fragment.Build(attributeName, AttributeScalarValueText.BuildFromString(jo.Value<string>()));
        }
        private CICandidateAttributeData.Fragment JValue2TextArrayAttribute(JToken o, string jsonName, string attributeName = null)
        {
            return CICandidateAttributeData.Fragment.Build(attributeName ?? jsonName, AttributeArrayValueText.BuildFromString(o[jsonName].Values<string>().ToArray()));
        }
        private CICandidateAttributeData.Fragment JArray2JSONArrayAttribute(JArray array, string attributeName)
        {
            return CICandidateAttributeData.Fragment.Build(attributeName, AttributeArrayValueJSON.Build(array.ToArray()));
        }
        private CICandidateAttributeData.Fragment JToken2JSONAttribute(JToken o, string attributeName)
        {
            return CICandidateAttributeData.Fragment.Build(attributeName, AttributeScalarValueJSON.Build(o));
        }
    }
}
