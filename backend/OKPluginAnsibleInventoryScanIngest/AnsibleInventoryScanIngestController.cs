using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Controllers.Ingest
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Ingest/AnsibleInventoryScan")]
    [Authorize]
    public class AnsibleInventoryScanIngestController : ControllerBase
    {
        private readonly IngestDataService ingestDataService;
        private readonly ILayerModel layerModel;
        private readonly ILogger<AnsibleInventoryScanIngestController> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILayerBasedAuthorizationService authorizationService;

        public AnsibleInventoryScanIngestController(IngestDataService ingestDataService, ILayerModel layerModel, ICurrentUserService currentUserService,
            IModelContextBuilder modelContextBuilder,
            ILayerBasedAuthorizationService authorizationService, ILogger<AnsibleInventoryScanIngestController> logger)
        {
            this.ingestDataService = ingestDataService;
            this.layerModel = layerModel;
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.modelContextBuilder = modelContextBuilder;
            this.authorizationService = authorizationService;
        }

        [HttpPost("")]
        public async Task<ActionResult> IngestAnsibleInventoryScan([FromQuery, Required] long writeLayerID, [FromQuery, Required] long[] searchLayerIDs, [FromBody, Required] AnsibleInventoryScanDTO data)
        {
            try
            {
                using var mc = modelContextBuilder.BuildImmediate();
                var searchLayers = new LayerSet(searchLayerIDs);
                var writeLayer = await layerModel.GetLayer(writeLayerID, mc);
                var user = await currentUserService.GetCurrentUser(mc);

                if (writeLayer == null)
                {
                    return BadRequest($"Cannot write to layer with ID {writeLayerID}: layer does not exist");
                }

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

                    if (facts == null)
                    {
                        throw new Exception("Could not find ansible_facts element");
                    }

                    var tempCIID = Guid.NewGuid();
                    var fqdn = facts["ansible_fqdn"]?.Value<string>();
                    if (fqdn == null)
                    {
                        throw new Exception("Could not find ansible_fqdn element or invalid value");
                    }

                    var ciName = fqdn;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment?>()
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
                    }.WhereNotNull();
                    var attributes = new CICandidateAttributeData(attributeFragments);
                    var ciCandidate = new CICandidate(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes);
                    cis.Add(tempCIID, ciCandidate);

                    baseCIs.Add(fqdn, (tempCIID, ciCandidate));

                    // ansible mounts
                    var ansibleMounts = facts["ansible_mounts"];
                    if (ansibleMounts != null)
                        foreach (var mount in ansibleMounts)
                        {
                            var tempMountCIID = Guid.NewGuid();
                            var mountValue = mount["mount"]?.Value<string>() ?? "";
                            var ciNameMount = $"{fqdn}:{mountValue}";
                            var attributeFragmentsMount = new List<CICandidateAttributeData.Fragment?>
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
                            }.WhereNotNull();
                            var attributeData = new CICandidateAttributeData(attributeFragmentsMount);
                            cis.Add(tempMountCIID, new CICandidate(
                                // TODO: ansible mounts have an uuid, find out what that is and if they can be used for identification
                                CIIdentificationMethodByData.BuildFromAttributes(new string[] { "device", "mount", ICIModel.NameAttribute }, attributeData, searchLayers), // TODO: do not use CIModel.NameAttribute, rather maybe use its relation to the host for identification
                                attributeData));

                            relations.Add(new RelationCandidate(
                                CIIdentificationMethodByTemporaryCIID.Build(tempCIID),
                                CIIdentificationMethodByTemporaryCIID.Build(tempMountCIID),
                                "has_mounted_device"));
                        }

                    // ansible interfaces
                    var ansibleInterfaces = facts["ansible_interfaces"];
                    if (ansibleInterfaces != null)
                        foreach (var interfaceName in ansibleInterfaces.Values<string>())
                        {
                            var jsonTokenName = $"ansible_{interfaceName.Replace('-', '_')}"; // TODO: ansible seems to convert - to _ for some reason... find out what else!
                            var @interface = facts[jsonTokenName] ?? "";
                            var tempCIIDInterface = Guid.NewGuid();
                            var ciNameInterface = $"Network Interface {interfaceName}@{fqdn}";
                            var attributeFragmentsInterface = new List<CICandidateAttributeData.Fragment?>
                            {
                                JValue2TextAttribute(@interface, "device"),
                                JValue2TextAttribute(@interface, "active"),
                                JValue2TextAttribute(@interface, "type"),
                                Try2(() => JValue2TextAttribute(@interface, "macaddress")),
                                // TODO
                                String2Attribute(ICIModel.NameAttribute, ciNameInterface)
                            }.WhereNotNull();
                            var attributeData = new CICandidateAttributeData(attributeFragmentsInterface);
                            cis.Add(tempCIIDInterface, new CICandidate(
                                CIIdentificationMethodByData.BuildFromAttributes(new string[] { ICIModel.NameAttribute }, attributeData, searchLayers), // TODO: do not use CIModel.NameAttribute, rather maybe use its relation to the host for identification
                                attributeData));

                            relations.Add(new RelationCandidate(
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

                    var attributeFragments = new List<CICandidateAttributeData.Fragment?>()
                    {
                        JToken2JSONAttribute(kvInstalled.Value["results"], "yum.installed"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    }.WhereNotNull();
                    var attributes = new CICandidateAttributeData(attributeFragments);

                    if (baseCIs.TryGetValue(fqdn, out var @base))
                    { // attach to base CI
                        cis[@base.ciid] = CICandidate.BuildWithAdditionalAttributes(@base.ci, attributes);
                    }
                    else
                    { // treat as new CI
                        cis.Add(Guid.NewGuid(), new CICandidate(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes));
                    }
                }
                foreach (var kvRepos in data.YumRepos)
                {
                    var hostID = kvRepos.Key;
                    var fqdn = hostID; // TODO: check if using the HostID as fqdn is ok
                    var ciName = hostID;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment?>()
                    {
                        JToken2JSONAttribute(kvRepos.Value["results"], "yum.repos"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    }.WhereNotNull();
                    var attributes = new CICandidateAttributeData(attributeFragments);

                    if (baseCIs.TryGetValue(fqdn, out var @base))
                    { // attach to base CI
                        cis[@base.ciid] = CICandidate.BuildWithAdditionalAttributes(@base.ci, attributes);
                    }
                    else
                    { // treat as new CI
                        cis.Add(Guid.NewGuid(), new CICandidate(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes));
                    }
                }
                foreach (var kvUpdates in data.YumUpdates)
                {
                    var hostID = kvUpdates.Key;
                    var fqdn = hostID; // TODO: check if using the HostID as fqdn is ok
                    var ciName = hostID;

                    var attributeFragments = new List<CICandidateAttributeData.Fragment?>()
                    {
                        JToken2JSONAttribute(kvUpdates.Value["results"], "yum.updates"),
                        String2Attribute(ICIModel.NameAttribute, ciName),
                        String2Attribute("fqdn", fqdn)
                    }.WhereNotNull();
                    var attributes = new CICandidateAttributeData(attributeFragments);

                    if (baseCIs.TryGetValue(fqdn, out var @base))
                    { // attach to base CI
                        cis[@base.ciid] = CICandidate.BuildWithAdditionalAttributes(@base.ci, attributes);
                    }
                    else
                    { // treat as new CI
                        cis.Add(Guid.NewGuid(), new CICandidate(CIIdentificationMethodByData.BuildFromAttributes(new string[] { "fqdn" }, attributes, searchLayers), attributes));
                    }
                }

                var ingestData = new IngestData(cis, relations);
                var (numIngestedCIs, numIngestedRelations) = await ingestDataService.Ingest(ingestData, writeLayer, user, modelContextBuilder, logger);

                logger.LogInformation($"Ansible Ingest successful; ingested {numIngestedCIs} CIs, {numIngestedRelations} relations");

                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ansible Ingest failed");
                return BadRequest();
            }
        }

        private CICandidateAttributeData.Fragment? Try2(Func<CICandidateAttributeData.Fragment?> f)
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
            new CICandidateAttributeData.Fragment(name, new AttributeScalarValueText(value));
        private CICandidateAttributeData.Fragment String2IntegerAttribute(string name, long value) =>
            new CICandidateAttributeData.Fragment(name, new AttributeScalarValueInteger(value));

        private CICandidateAttributeData.Fragment? JValue2TextAttribute(JToken o, string jsonName, string? attributeName = null)
        {
            var v = o[jsonName]?.Value<string>();
            if (v == null) return null;
            return new CICandidateAttributeData.Fragment(attributeName ?? jsonName, new AttributeScalarValueText(v));
        }
        private CICandidateAttributeData.Fragment? JValue2IntegerAttribute(JToken o, string name, string? attributeName = null)
        {
            var v = o[name]?.Value<long>();
            if (!v.HasValue) return null;
            return new CICandidateAttributeData.Fragment(attributeName ?? name, new AttributeScalarValueInteger(v.Value));
        }
        private CICandidateAttributeData.Fragment? JValue2JSONAttribute(JToken o, string jsonName, string? attributeName = null)
        {
            var v = o[jsonName];
            if (v == null) return null;
            return new CICandidateAttributeData.Fragment(attributeName ?? jsonName, AttributeScalarValueJSON.Build(v));
        }
        private CICandidateAttributeData.Fragment? JValuePath2TextAttribute(JToken o, string jsonPath, string attributeName)
        {
            var v = o.SelectToken(jsonPath)?.Value<string>();
            if (v == null) return null;
            return new CICandidateAttributeData.Fragment(attributeName, new AttributeScalarValueText(v));
        }
        private CICandidateAttributeData.Fragment? JValue2TextArrayAttribute(JToken o, string jsonName, string? attributeName = null)
        {
            var v = o[jsonName]?.Values<string>().ToArray();
            if (v == null) return null;
            return new CICandidateAttributeData.Fragment(attributeName ?? jsonName, AttributeArrayValueText.BuildFromString(v));
        }
        private CICandidateAttributeData.Fragment JArray2JSONArrayAttribute(JArray array, string attributeName)
        {
            return new CICandidateAttributeData.Fragment(attributeName, AttributeArrayValueJSON.Build(array.ToArray()));
        }
        private CICandidateAttributeData.Fragment? JToken2JSONAttribute(JToken? o, string attributeName)
        {
            if (o == null) return null;
            return new CICandidateAttributeData.Fragment(attributeName, AttributeScalarValueJSON.Build(o));
        }
    }
}
