using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tasks.DBInit
{
    [Explicit]
    class DBInit
    {

        [Test]
        public async Task Run()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build("landscape_prototype", false, true);

            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var traitModel = new RecursiveTraitModel(NullLogger<RecursiveTraitModel>.Instance, conn);

            var random = new Random(3);

            var user = await DBSetup.SetupUser(userModel, "init-user", new Guid("3544f9a7-cc17-4cba-8052-f88656cf1ef1"));

            await traitModel.SetRecursiveTraitSet(DefaultTraits.Get(), null);

            var numApplicationCIs = 1;
            var numHostCIs = 1;
            var numRunsOnRelations = 1;
            int numAttributesPerCIFrom = 20;
            int numAttributesPerCITo = 40;
            //var regularTypeIDs = new[] { "Host Linux", "Host Windows", "Application" };
            var predicateRunsOn = Predicate.Build("runs_on", "runs on", "is running", AnchorState.Active, PredicateModel.DefaultConstraits);
            
            //var regularPredicates = new[] {
            //Predicate.Build("is_part_of", "is part of", "has part", AnchorState.Active),
            //Predicate.Build("is_attached_to", "is attached to", "has attachment", AnchorState.Active),
            //};
            var regularAttributeNames = new[] { "att_1", "att_2", "att_3", "att_4", "att_5", "att_6", "att_7", "att_8", "att_9" };
            var regularAttributeValues = Enumerable.Range(0, 10).Select<int, IAttributeValue>(i =>
            {
                var r = random.Next(6);
                if (r == 0)
                    return AttributeScalarValueInteger.Build(random.Next(1000));
                else if (r == 1)
                    return AttributeArrayValueText.BuildFromString(Enumerable.Range(0, random.Next(1, 5)).Select(i => $"value_{i}").ToArray());
                else
                    return AttributeScalarValueText.BuildFromString($"attribute value {i + 1}");
            }).ToArray();

            var applicationCIIDs = Enumerable.Range(0, numApplicationCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();
            var hostCIIDs = Enumerable.Range(0, numHostCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            var monitoringPredicates = new[] {
                Predicate.Build("has_monitoring_module", "has monitoring module", "is assigned to", AnchorState.Active, PredicateModel.DefaultConstraits),
                Predicate.Build("is_monitored_by", "is monitored by", "monitors", AnchorState.Active, PredicateModel.DefaultConstraits),
                Predicate.Build("belongs_to_naemon_contactgroup", "belongs to naemon contactgroup", "has member", AnchorState.Active, PredicateModel.DefaultConstraits)
            };

            var baseDataPredicates = new[] {
                Predicate.Build("member_of_group", "is member of group", "has member", AnchorState.Active, PredicateModel.DefaultConstraits),
                Predicate.Build("managed_by", "is managed by", "manages", AnchorState.Active, PredicateModel.DefaultConstraits)
            };

            // create layers
            long cmdbLayerID;
            long monitoringDefinitionsLayerID;
            long activeDirectoryLayerID;
            using (var trans = conn.BeginTransaction())
            {
                var cmdbLayer = await layerModel.CreateLayer("CMDB", Color.Blue, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                cmdbLayerID = cmdbLayer.ID;
                await layerModel.CreateLayer("Inventory Scan", Color.Violet, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                var monitoringDefinitionsLayer = await layerModel.CreateLayer("Monitoring Definitions", Color.Orange, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                monitoringDefinitionsLayerID = monitoringDefinitionsLayer.ID;
                await layerModel.CreateLayer("Monitoring", ColorTranslator.FromHtml("#FFE6CC"), AnchorState.Active, ComputeLayerBrainLink.Build("OKPluginCLBMonitoring.CLBNaemonMonitoring"), OnlineInboundAdapterLink.Build(""), trans);
                var automationLayer = await layerModel.CreateLayer("Active Directory", Color.Cyan, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                activeDirectoryLayerID = automationLayer.ID;
                trans.Commit();
            }

            // create regular CIs
            var windowsHostCIIds = new List<Guid>();
            var linuxHostCIIds = new List<Guid>();
            using (var trans = conn.BeginTransaction())
            {
                var index = 0;
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                foreach (var ciid in applicationCIIDs)
                {
                    await ciModel.CreateCI(ciid, trans);
                    await attributeModel.InsertCINameAttribute($"Application_{index}", ciid, cmdbLayerID, changeset, trans); 
                    await attributeModel.InsertAttribute("application_name", AttributeScalarValueText.BuildFromString($"Application_{index}"), ciid, cmdbLayerID, changeset, trans);
                    index++;
                }
                index = 0;
                foreach (var ciid in hostCIIDs)
                {
                    var ciType = new string[] { "Host Linux", "Host Windows" }.GetRandom(random);
                    var hostCIID = await ciModel.CreateCI(ciid, trans);
                    if (ciType.Equals("Host Linux"))
                        linuxHostCIIds.Add(hostCIID);
                    else
                        windowsHostCIIds.Add(hostCIID);
                    await attributeModel.InsertCINameAttribute($"{ciType}_{index}", ciid, cmdbLayerID, changeset, trans);
                    await attributeModel.InsertAttribute("hostname", AttributeScalarValueText.BuildFromString($"hostname_{index}.domain"), ciid, cmdbLayerID, changeset, trans);
                    await attributeModel.InsertAttribute("system", AttributeScalarValueText.BuildFromString($"{((ciType.Equals("Host Linux")) ? "Linux" : "Windows")}"), ciid, cmdbLayerID, changeset, trans);
                    index++;
                }

                trans.Commit();
            }

            // create predicates
            using (var trans = conn.BeginTransaction())
            {
                foreach (var predicate in new Predicate[] { predicateRunsOn }.Concat(monitoringPredicates).Concat(baseDataPredicates))
                    await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, AnchorState.Active, PredicateModel.DefaultConstraits, trans);

                trans.Commit();
            }

            // create regular attributes
            foreach (var ciid in applicationCIIDs)
            {
                var numAttributeChanges = random.Next(numAttributesPerCIFrom, numAttributesPerCITo);
                for (int i = 0; i < numAttributeChanges; i++)
                {
                    using var trans = conn.BeginTransaction();
                    var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                    var name = regularAttributeNames.GetRandom(random);
                    var value = regularAttributeValues.GetRandom(random);
                    await attributeModel.InsertAttribute(name, value, ciid, cmdbLayerID, changeset, trans);
                    // TODO: attribute removals
                    trans.Commit();
                }
            }

            // create runs on predicates
            for (var i = 0; i < numRunsOnRelations; i++)
            {
                using var trans = conn.BeginTransaction();
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var ciid1 = applicationCIIDs.GetRandom(random);
                var ciid2 = hostCIIDs.Except(new[] { ciid1 }).GetRandom(random); // TODO, HACK: slow
                await relationModel.InsertRelation(ciid1, ciid2, predicateRunsOn.ID, cmdbLayerID, changeset, trans);
                trans.Commit();
            }

            // build monitoring things
            Guid ciMonModuleHost;
            Guid ciMonModuleHostWindows;
            Guid ciMonModuleHostLinux;
            Guid ciNaemon01;
            Guid ciNaemon02;
            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                ciNaemon01 = await ciModel.CreateCI(null);
                ciNaemon02 = await ciModel.CreateCI(null);
                await attributeModel.InsertCINameAttribute("Naemon Instance 01", ciNaemon01, cmdbLayerID, changeset, trans);
                await attributeModel.InsertCINameAttribute("Naemon Instance 02", ciNaemon02, cmdbLayerID, changeset, trans);
                await attributeModel.InsertAttribute("naemon.instance_name", AttributeScalarValueText.BuildFromString("Naemon Instance 01"), ciNaemon01, monitoringDefinitionsLayerID, changeset, trans);
                await attributeModel.InsertAttribute("naemon.instance_name", AttributeScalarValueText.BuildFromString("Naemon Instance 02"), ciNaemon02, monitoringDefinitionsLayerID, changeset, trans);
                //await attributeModel.InsertAttribute("ipAddress", AttributeValueTextScalar.Build("1.2.3.4"), cmdbLayerID, ciNaemon01, changeset.ID, trans);
                //await attributeModel.InsertAttribute("ipAddress", AttributeValueTextScalar.Build("4.5.6.7"), cmdbLayerID, ciNaemon02, changeset.ID, trans);

                ciMonModuleHost = await ciModel.CreateCI(null);
                ciMonModuleHostWindows = await ciModel.CreateCI(null);
                ciMonModuleHostLinux = await ciModel.CreateCI(null);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host", ciMonModuleHost, monitoringDefinitionsLayerID, changeset, trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host Windows", ciMonModuleHostWindows, monitoringDefinitionsLayerID, changeset, trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host Linux", ciMonModuleHostLinux, monitoringDefinitionsLayerID, changeset, trans);
                await attributeModel.InsertAttribute("naemon.config_template",
                    AttributeScalarValueText.BuildFromString(@"[{
  ""type"": ""host"",
  ""contactgroupSource"": ""{{ target.id }}"",
  ""command"": {
                    ""executable"": ""check_ping"",
    ""parameters"": ""-H {{ target.attributes.hostname }} -w {{ target.attributes.naemon.variables.host.warning_threshold ?? ""3000.0,80 % "" }} -c 5000.0,100% -p 5""
  }
            }]", true), ciMonModuleHost, monitoringDefinitionsLayerID, changeset, trans);
                await attributeModel.InsertAttribute("naemon.config_template",
                    AttributeScalarValueText.BuildFromString(
@"{{~ for related_ci in target.relations.back.runs_on ~}}
[{
  ""type"": ""service"",
  ""contactgroupSource"": ""{{ related_ci.id }}"",
  ""description"": ""service check windows application"",
  ""command"": {
                    ""executable"": ""check_command"",
    ""parameters"": ""-application_name {{ related_ci.attributes.application_name}}""
  }
}]
{{~ end ~}}
"
                    , true), ciMonModuleHostWindows, monitoringDefinitionsLayerID, changeset, trans);
                await attributeModel.InsertAttribute("naemon.config_template",
                    AttributeScalarValueText.BuildFromString(
@"{{~ for related_ci in target.relations.back.runs_on ~}}
[{
  ""type"": ""service"",
  ""contactgroupSource"": ""{{ related_ci.id }}"",
  ""description"": ""service check linux application"",
  ""command"": {
                    ""executable"": ""check_command"",
    ""parameters"": ""-application_name {{ related_ci.attributes.application_name}}""
  }
}]
{{~ end ~}}
            "
                        , true), ciMonModuleHostLinux, monitoringDefinitionsLayerID, changeset, trans);
                trans.Commit();
            }

            // create monitoring relations
            if (!windowsHostCIIds.IsEmpty())
            {
                var windowsHosts = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(windowsHostCIIds), await layerModel.BuildLayerSet(new[] { "CMDB" }, null), true, null, TimeThreshold.BuildLatest());
                foreach (var ci in windowsHosts)
                {
                    using var trans = conn.BeginTransaction();
                    var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                    await relationModel.InsertRelation(ci.ID, ciMonModuleHost, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                    await relationModel.InsertRelation(ci.ID, ciMonModuleHostWindows, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                    trans.Commit();
                }
            }
            if (!linuxHostCIIds.IsEmpty())
            {
                var linuxHosts = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(linuxHostCIIds), await layerModel.BuildLayerSet(new[] { "CMDB" }, null), true, null, TimeThreshold.BuildLatest());
                foreach (var ci in linuxHosts)
                {
                    using var trans = conn.BeginTransaction();
                    var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                    await relationModel.InsertRelation(ci.ID, ciMonModuleHost, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                    await relationModel.InsertRelation(ci.ID, ciMonModuleHostLinux, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                    trans.Commit();
                }
            }

            // create ansible groups
            //Guid ciAutomationAnsibleHostGroupTest;
            //Guid ciAutomationAnsibleHostGroupTest2;
            //using (var trans = conn.BeginTransaction())
            //{
            //    var changeset = await changesetModel.CreateChangeset(user.ID, trans);
            //    ciAutomationAnsibleHostGroupTest = await ciModel.CreateCIWithType("Ansible Host Group", null);
            //    ciAutomationAnsibleHostGroupTest2 = await ciModel.CreateCIWithType("Ansible Host Group", null);
            //    await attributeModel.InsertCINameAttribute("Ansible Host Group Test", automationLayerID, ciAutomationAnsibleHostGroupTest, changeset.ID, trans);
            //    await attributeModel.InsertCINameAttribute("Ansible Host Group Test2", automationLayerID, ciAutomationAnsibleHostGroupTest2, changeset.ID, trans);
            //    await attributeModel.InsertAttribute("automation.ansible_group_name", AttributeValueTextScalar.Build("test_group"), automationLayerID, ciAutomationAnsibleHostGroupTest, changeset.ID, trans);
            //    await attributeModel.InsertAttribute("automation.ansible_group_name", AttributeValueTextScalar.Build("test_group2"), automationLayerID, ciAutomationAnsibleHostGroupTest2, changeset.ID, trans);
            //    trans.Commit();
            //}

            //// assign ansible groups
            //using (var trans = conn.BeginTransaction())
            //{
            //    var changeset = await changesetModel.CreateChangeset(user.ID, trans);
            //    await relationModel.InsertRelation(ciNaemon01, ciAutomationAnsibleHostGroupTest, "has_ansible_group", automationLayerID, changeset.ID, trans);
            //    await relationModel.InsertRelation(ciNaemon02, ciAutomationAnsibleHostGroupTest, "has_ansible_group", automationLayerID, changeset.ID, trans);
            //    await relationModel.InsertRelation(ciNaemon02, ciAutomationAnsibleHostGroupTest2, "has_ansible_group", automationLayerID, changeset.ID, trans);
            //    trans.Commit();
            //}
        }
    }

    public static class DefaultTraits
    {
        public static RecursiveTraitSet Get()
        {
            return RecursiveTraitSet.Build(
                    // hosts
                    RecursiveTrait.Build("host", new List<TraitAttribute>() {
                        TraitAttribute.Build("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    RecursiveTrait.Build("windows_host", new List<TraitAttribute>() {
                        TraitAttribute.Build("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                CIAttributeValueConstraintTextRegex.Build(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    RecursiveTrait.Build("linux_host", new List<TraitAttribute>() {
                        TraitAttribute.Build("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                CIAttributeValueConstraintTextRegex.Build(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    RecursiveTrait.Build("linux_block_device", new List<TraitAttribute>() {
                        TraitAttribute.Build("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("mount",
                            CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // linux network_interface
                    RecursiveTrait.Build("linux_network_interface", new List<TraitAttribute>() {
                        TraitAttribute.Build("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("type",
                            CIAttributeTemplate.BuildFromParams("type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("active",
                            CIAttributeTemplate.BuildFromParams("active", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // applications
                    RecursiveTrait.Build("application", new List<TraitAttribute>() {
                        TraitAttribute.Build("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    RecursiveTrait.Build("ansible_can_deploy_to_it",
                        new List<TraitAttribute>() {
                            TraitAttribute.Build("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                                CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },
                        new List<TraitAttribute>() {
                            TraitAttribute.Build("variables",
                                CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                            )
                        },
                        new List<TraitRelation>() {
                            TraitRelation.Build("ansible_groups",
                                RelationTemplate.Build("has_ansible_group", 1, null)
                            )
                        }
                    )
                );
        }
    }
}
