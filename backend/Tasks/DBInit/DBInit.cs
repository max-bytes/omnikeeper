using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
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
            using var conn = dbcb.BuildFromUserSecrets(GetType().Assembly, true);

            var partitionModel = new PartitionModel();
            var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
            var baseAttributeModel = new BaseAttributeModel(partitionModel, new CIIDModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var baseRelationModel = new BaseRelationModel(partitionModel);
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var relationModel = new RelationModel(new BaseRelationModel(partitionModel));
            var effectiveTraitModel = new EffectiveTraitModel(relationModel, NullLogger<EffectiveTraitModel>.Instance);
            var predicateModel = new PredicateModel(effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel);
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var traitModel = new RecursiveDataTraitModel(effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel);
            var lbas = new Mock<ILayerBasedAuthorizationService>();
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<AuthenticatedUser>(), It.IsAny<Layer>())).Returns(true);
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());

            var random = new Random(3);


            var mc = modelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, mc, "init-user", new Guid("3544f9a7-cc17-4cba-8052-f88656cf1ef1"));
            var authenticatedUser = new AuthenticatedUser(user, new HashSet<string>() { });

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(mc);

            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                foreach (var rt in DefaultTraits.Get())
                {
                    await traitModel.InsertOrUpdate(rt.ID, rt.RequiredAttributes, rt.OptionalAttributes, rt.RequiredRelations, rt.OptionalRelations, rt.RequiredTraits, 
                        new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changeset, mc);
                }
                trans.Commit();
            }

            var numApplicationCIs = 0;
            var numHostCIs = 0;
            var numRunsOnRelations = 0;
            int numAttributesPerCIFrom = 20;
            int numAttributesPerCITo = 40;
            //var regularTypeIDs = new[] { "Host Linux", "Host Windows", "Application" };
            var predicateRunsOn = new Predicate("runs_on", "runs on", "is running");

            //var regularPredicates = new[] {
            //new Predicate("is_part_of", "is part of", "has part", AnchorState.Active),
            //new Predicate("is_attached_to", "is attached to", "has attachment", AnchorState.Active),
            //};
            //var regularAttributeNames = new[] { "att_1", "att_2", "att_3", "att_4", "att_5", "att_6", "att_7", "att_8", "att_9" };
            //var regularAttributeValues = Enumerable.Range(0, 10).Select<int, IAttributeValue>(i =>
            //{
            //    var r = random.Next(6);
            //    if (r == 0)
            //        return new AttributeScalarValueInteger(random.Next(1000));
            //    else if (r == 1)
            //        return AttributeArrayValueText.BuildFromString(Enumerable.Range(0, random.Next(1, 5)).Select(i => $"value_{i}").ToArray());
            //    else
            //        return new AttributeScalarValueText($"attribute value {i + 1}");
            //}).ToArray();
            Func<IAttributeValue> randomAttributeValue = () => new AttributeScalarValueText("V" + RandomUtility.GenerateRandomString(8, random));
            var possibleAttributes = new ((string name, Func<IAttributeValue> value), int chance)[]
            {
                //(("hostname", randomAttributeValue), 5),
                //(("ipAddress", randomAttributeValue), 1),
                //(("application_name", randomAttributeValue), 1),
                //(("os_family", () => new AttributeScalarValueText(RandomUtility.GetRandom(random, ("Windows", 10), ("Redhat", 3), ("Gentoo", 1)))), 5),
                //(("device", randomAttributeValue), 1),
                //(("mount", randomAttributeValue), 1),
                //(("type", randomAttributeValue), 1),
                //(("active", randomAttributeValue), 1),
                (("generic-attribute 1", randomAttributeValue), 1),
                (("generic-attribute 2", randomAttributeValue), 1),
                (("generic-attribute 3", randomAttributeValue), 1),
                (("generic-attribute 4", randomAttributeValue), 1),
                (("generic-attribute 5", randomAttributeValue), 1),
            };

            var applicationCIIDs = Enumerable.Range(0, numApplicationCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();
            var hostCIIDs = Enumerable.Range(0, numHostCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            var monitoringPredicates = new[] {
                new Predicate("has_monitoring_module", "has monitoring module", "is assigned to"),
                new Predicate("is_monitored_by", "is monitored by", "monitors"),
                new Predicate("belongs_to_naemon_contactgroup", "belongs to naemon contactgroup", "has member")
            };

            var baseDataPredicates = new[] {
                new Predicate("member_of_group", "is member of group", "has member"),
                new Predicate("managed_by", "is managed by", "manages")
            };

            // create layers
            string cmdbLayerID;
            string monitoringDefinitionsLayerID;
            string activeDirectoryLayerID;
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var configWriteLayer = await layerModel.UpsertLayer("Config", "", Color.Blue, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                var cmdbLayer = await layerModel.UpsertLayer("CMDB", "", Color.Blue, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                cmdbLayerID = cmdbLayer.ID;
                await layerModel.UpsertLayer("Inventory Scan", "", Color.Violet, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                var monitoringDefinitionsLayer = await layerModel.UpsertLayer("Monitoring Definitions", "", Color.Orange, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                monitoringDefinitionsLayerID = monitoringDefinitionsLayer.ID;
                await layerModel.UpsertLayer("Monitoring", "", ColorTranslator.FromHtml("#FFE6CC"), AnchorState.Active, ComputeLayerBrainLink.Build("OKPluginCLBMonitoring.CLBNaemonMonitoring"), OnlineInboundAdapterLink.Build(""), trans);
                var automationLayer = await layerModel.UpsertLayer("Active Directory", "", Color.Cyan, AnchorState.Active, ComputeLayerBrainLink.Build(""), OnlineInboundAdapterLink.Build(""), trans);
                activeDirectoryLayerID = automationLayer.ID;
                trans.Commit();
            }

            // create regular CIs
            var windowsHostCIIds = new HashSet<Guid>();
            var linuxHostCIIds = new HashSet<Guid>();
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var index = 0;
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

                await ciModel.BulkCreateCIs(applicationCIIDs, trans);
                await ciModel.BulkCreateCIs(hostCIIDs, trans);

                var fragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
                foreach (var ciid in applicationCIIDs)
                {
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText($"Application_{index}"), ciid));
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("application_name", new AttributeScalarValueText($"Application_{index}"), ciid));
                    //await attributeModel.InsertCINameAttribute($"Application_{index}", ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    //await attributeModel.InsertAttribute("application_name", new AttributeScalarValueText($"Application_{index}"), ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    index++;
                }
                index = 0;
                foreach (var ciid in hostCIIDs)
                {
                    var ciType = new string[] { "Host Linux", "Host Windows" }.GetRandom(random);
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment(ICIModel.NameAttribute, new AttributeScalarValueText($"{ciType}_{index}"), ciid));
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("hostname", new AttributeScalarValueText($"hostname_{index}.domain"), ciid));
                    //await attributeModel.InsertCINameAttribute($"{ciType}_{index}", ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    //await attributeModel.InsertAttribute("hostname", new AttributeScalarValueText($"hostname_{index}.domain"), ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    //await attributeModel.InsertAttribute("system", new AttributeScalarValueText($"{((ciType.Equals("Host Linux")) ? "Linux" : "Windows")}"), ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                    if (ciType.Equals("Host Linux"))
                    {
                        linuxHostCIIds.Add(ciid);
                        fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("system", new AttributeScalarValueText($"Linux"), ciid));
                        fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("os_family", new AttributeScalarValueText(RandomUtility.GetRandom(random, ("Redhat", 3), ("Gentoo", 1))), ciid));
                        //await attributeModel.InsertAttribute("os.family", new AttributeScalarValueText(RandomUtility.GetRandom(random, ("Redhat", 3), ("Gentoo", 1))), ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    }
                    else
                    {
                        windowsHostCIIds.Add(ciid);
                        fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("system", new AttributeScalarValueText($"Windows"), ciid));
                        fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("os_family", new AttributeScalarValueText(RandomUtility.GetRandom(random, ("Windows 7", 10), ("Windows XP", 3), ("Windows 10", 20))), ciid));
                        //await attributeModel.InsertAttribute("os.family", new AttributeScalarValueText(RandomUtility.GetRandom(random, ("Windows 7", 10), ("Windows XP", 3), ("Windows 10", 20))), ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    }

                    index++;
                }

                await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataLayerScope("", cmdbLayerID, fragments), changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            // create predicates
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                foreach (var predicate in new Predicate[] { predicateRunsOn }.Concat(monitoringPredicates).Concat(baseDataPredicates))
                {
                    await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo,
                        new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changeset, trans);
                }

                trans.Commit();
            }

            // create regular attributes
            foreach (var ciid in applicationCIIDs)
            {
                var numAttributeChanges = random.Next(numAttributesPerCIFrom, numAttributesPerCITo);
                for (int i = 0; i < numAttributeChanges; i++)
                {
                    using var trans = modelContextBuilder.BuildDeferred();
                    var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                    var nameValue = RandomUtility.GetRandom(random, possibleAttributes);
                    var name = nameValue.name;
                    var value = nameValue.value();
                    await attributeModel.InsertAttribute(name, value, ciid, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    // TODO: attribute removals
                    trans.Commit();
                }
            }

            // create runs on predicates
            for (var i = 0; i < numRunsOnRelations; i++)
            {
                using var trans = modelContextBuilder.BuildDeferred();
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var ciid1 = applicationCIIDs.GetRandom(random);
                var ciid2 = hostCIIDs.Except(new[] { ciid1 }).GetRandom(random); // TODO, HACK: slow
                await relationModel.InsertRelation(ciid1, ciid2, predicateRunsOn.ID, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            // build monitoring things
            Guid ciMonModuleHost;
            Guid ciMonModuleHostWindows;
            Guid ciMonModuleHostLinux;
            Guid ciNaemon01;
            Guid ciNaemon02;
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                ciNaemon01 = await ciModel.CreateCI(trans);
                ciNaemon02 = await ciModel.CreateCI(trans);
                await attributeModel.InsertCINameAttribute("Naemon Instance 01", ciNaemon01, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("Naemon Instance 02", ciNaemon02, cmdbLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("naemon.instance_name", new AttributeScalarValueText("Naemon Instance 01"), ciNaemon01, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("naemon.instance_name", new AttributeScalarValueText("Naemon Instance 02"), ciNaemon02, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                //await attributeModel.InsertAttribute("ipAddress", AttributeValueTextScalar.Build("1.2.3.4"), cmdbLayerID, ciNaemon01, changeset.ID, trans);
                //await attributeModel.InsertAttribute("ipAddress", AttributeValueTextScalar.Build("4.5.6.7"), cmdbLayerID, ciNaemon02, changeset.ID, trans);

                ciMonModuleHost = await ciModel.CreateCI(trans);
                ciMonModuleHostWindows = await ciModel.CreateCI(trans);
                ciMonModuleHostLinux = await ciModel.CreateCI(trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host", ciMonModuleHost, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host Windows", ciMonModuleHostWindows, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host Linux", ciMonModuleHostLinux, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("naemon.config_template",
                    new AttributeScalarValueText(@"[{
  ""type"": ""host"",
  ""contactgroupSource"": ""{{ target.id }}"",
  ""command"": {
                    ""executable"": ""check_ping"",
    ""parameters"": ""-H {{ target.attributes.hostname }} -w {{ target.attributes.naemon.variables.host.warning_threshold ?? ""3000.0,80 % "" }} -c 5000.0,100% -p 5""
  }
            }]", true), ciMonModuleHost, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("naemon.config_template",
                    new AttributeScalarValueText(
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
                    , true), ciMonModuleHostWindows, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("naemon.config_template",
                    new AttributeScalarValueText(
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
                        , true), ciMonModuleHostLinux, monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            // create monitoring relations
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var fragments = new List<BulkRelationDataLayerScope.Fragment>();
                if (!windowsHostCIIds.IsEmpty())
                {
                    var windowsHosts = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(windowsHostCIIds), await layerModel.BuildLayerSet(new[] { "CMDB" }, trans), true, AllAttributeSelection.Instance, trans, TimeThreshold.BuildLatest());
                    foreach (var ci in windowsHosts)
                    {
                        fragments.Add(new BulkRelationDataLayerScope.Fragment(ci.ID, ciMonModuleHost, "has_monitoring_module"));
                        fragments.Add(new BulkRelationDataLayerScope.Fragment(ci.ID, ciMonModuleHostWindows, "has_monitoring_module"));
                        //await relationModel.InsertRelation(ci.ID, ciMonModuleHost, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                        //await relationModel.InsertRelation(ci.ID, ciMonModuleHostWindows, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    }
                }
                if (!linuxHostCIIds.IsEmpty())
                {
                    var linuxHosts = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(linuxHostCIIds), await layerModel.BuildLayerSet(new[] { "CMDB" }, trans), true, AllAttributeSelection.Instance, trans, TimeThreshold.BuildLatest());
                    foreach (var ci in linuxHosts)
                    {
                        fragments.Add(new BulkRelationDataLayerScope.Fragment(ci.ID, ciMonModuleHost, "has_monitoring_module"));
                        fragments.Add(new BulkRelationDataLayerScope.Fragment(ci.ID, ciMonModuleHostLinux, "has_monitoring_module"));
                        //await relationModel.InsertRelation(ci.ID, ciMonModuleHost, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                        //await relationModel.InsertRelation(ci.ID, ciMonModuleHostLinux, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                    }
                }
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await relationModel.BulkReplaceRelations(new BulkRelationDataLayerScope(cmdbLayerID, fragments), changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }


            // create ansible groups
            //Guid ciAutomationAnsibleHostGroupTest;
            //Guid ciAutomationAnsibleHostGroupTest2;
            //using (var trans = modelContextBuilder.BuildDeferred())
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
            //using (var trans = modelContextBuilder.BuildDeferred())
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
        public static IEnumerable<RecursiveTrait> Get()
        {
            return new List<RecursiveTrait>() {
                    // hosts
                    new RecursiveTrait("host", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    new RecursiveTrait("host_windows", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    new RecursiveTrait("host_linux", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    new RecursiveTrait("linux_block_device", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("mount",
                            CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // linux network_interface
                    new RecursiveTrait("linux_network_interface", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("type",
                            CIAttributeTemplate.BuildFromParams("type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("active",
                            CIAttributeTemplate.BuildFromParams("active", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // applications
                    new RecursiveTrait("application", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    new RecursiveTrait("ansible_can_deploy_to_it", new TraitOriginV1(TraitOriginType.Data),
                        new List<TraitAttribute>() {
                            new TraitAttribute("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                                CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },
                        new List<TraitAttribute>() {
                            new TraitAttribute("variables",
                                CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                            )
                        }
                        //new List<TraitRelation>() {
                        //    new TraitRelation("ansible_groups",
                        //        new RelationTemplate("has_ansible_group", 1, null)
                        //    )
                        //}
                    )
            };
        }
    }
}
