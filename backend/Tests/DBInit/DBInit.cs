using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;
using Tests.Integration.Model;
using Tests.Integration.Model.Mocks;

namespace Tests.DBInit
{
    [Explicit]
    [Ignore("Only manual")]
    class DBInit
    {

        [Test]
        public async Task Run()
        {
            DBSetup._Setup("landscape_prototype");

            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build("landscape_prototype", false, true);

            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));

            var random = new Random(3);

            var user = await DBSetup.SetupUser(userModel, "init-user", new Guid("3544f9a7-cc17-4cba-8052-f88656cf1ef1"));

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
                    return AttributeValueIntegerScalar.Build(random.Next(1000));
                else if (r == 1)
                    return AttributeArrayValueText.Build(Enumerable.Range(0, random.Next(1, 5)).Select(i => $"value_{i}").ToArray());
                else
                    return AttributeScalarValueText.Build($"attribute value {i + 1}");
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
                Predicate.Build("is_monitored_by", "is monitored by", "monitors", AnchorState.Active, PredicateModel.DefaultConstraits)
            };

            //var automationPredicates = new[] {
            //    Predicate.Build("has_ansible_group", "has ansible group", "is assigned to", AnchorState.Active)
            //};

            // create layers
            long cmdbLayerID;
            long monitoringDefinitionsLayerID;
            long automationLayerID;
            using (var trans = conn.BeginTransaction())
            {
                var cmdbLayer = await layerModel.CreateLayer("CMDB", trans);
                cmdbLayerID = cmdbLayer.ID;
                await layerModel.CreateLayer("Inventory Scan", trans);
                var monitoringDefinitionsLayer = await layerModel.CreateLayer("Monitoring Definitions", trans);
                monitoringDefinitionsLayerID = monitoringDefinitionsLayer.ID;
                await layerModel.CreateLayer("Monitoring", ColorTranslator.FromHtml("#FFE6CC"), AnchorState.Active, ComputeLayerBrainLink.Build("MonitoringPlugin.CLBNaemonMonitoring"), OnlineInboundAdapterLink.Build(""), trans);
                var automationLayer = await layerModel.CreateLayer("Automation", trans);
                automationLayerID = automationLayer.ID;
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
                    await ciModel.CreateCI(trans, ciid);
                    await attributeModel.InsertCINameAttribute($"Application_{index}", cmdbLayerID, ciid, changeset, trans); 
                    await attributeModel.InsertAttribute("application_name", AttributeScalarValueText.Build($"Application_{index}"), cmdbLayerID, ciid, changeset, trans);
                    index++;
                }
                index = 0;
                foreach (var ciid in hostCIIDs)
                {
                    var ciType = new string[] { "Host Linux", "Host Windows" }.GetRandom(random);
                    var hostCIID = await ciModel.CreateCI(trans, ciid);
                    if (ciType.Equals("Host Linux"))
                        linuxHostCIIds.Add(hostCIID);
                    else
                        windowsHostCIIds.Add(hostCIID);
                    await attributeModel.InsertCINameAttribute($"{ciType}_{index}", cmdbLayerID, ciid, changeset, trans);
                    await attributeModel.InsertAttribute("hostname", AttributeScalarValueText.Build($"hostname_{index}.domain"), cmdbLayerID, ciid, changeset, trans);
                    await attributeModel.InsertAttribute("system", AttributeScalarValueText.Build($"{((ciType.Equals("Host Linux")) ? "Linux" : "Windows")}"), cmdbLayerID, ciid, changeset, trans);
                    index++;
                }

                trans.Commit();
            }

            // create predicates
            using (var trans = conn.BeginTransaction())
            {
                foreach (var predicate in new Predicate[] { predicateRunsOn }.Concat(monitoringPredicates))//.Concat(automationPredicates))
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
                    await attributeModel.InsertAttribute(name, value, cmdbLayerID, ciid, changeset, trans);
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
                await attributeModel.InsertCINameAttribute("Naemon Instance 01", cmdbLayerID, ciNaemon01, changeset, trans);
                await attributeModel.InsertCINameAttribute("Naemon Instance 02", cmdbLayerID, ciNaemon02, changeset, trans);
                await attributeModel.InsertAttribute("monitoring.naemon.instance_name", AttributeScalarValueText.Build("Naemon Instance 01"), monitoringDefinitionsLayerID, ciNaemon01, changeset, trans);
                await attributeModel.InsertAttribute("monitoring.naemon.instance_name", AttributeScalarValueText.Build("Naemon Instance 02"), monitoringDefinitionsLayerID, ciNaemon02, changeset, trans);
                //await attributeModel.InsertAttribute("ipAddress", AttributeValueTextScalar.Build("1.2.3.4"), cmdbLayerID, ciNaemon01, changeset.ID, trans);
                //await attributeModel.InsertAttribute("ipAddress", AttributeValueTextScalar.Build("4.5.6.7"), cmdbLayerID, ciNaemon02, changeset.ID, trans);

                ciMonModuleHost = await ciModel.CreateCI(null);
                ciMonModuleHostWindows = await ciModel.CreateCI(null);
                ciMonModuleHostLinux = await ciModel.CreateCI(null);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host", monitoringDefinitionsLayerID, ciMonModuleHost, changeset, trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host Windows", monitoringDefinitionsLayerID, ciMonModuleHostWindows, changeset, trans);
                await attributeModel.InsertCINameAttribute("Monitoring Check Module Host Linux", monitoringDefinitionsLayerID, ciMonModuleHostLinux, changeset, trans);
                await attributeModel.InsertAttribute("monitoring.naemon.config_template",
                    AttributeScalarValueText.Build("check_host_cmd -ciid {{ target.ciid }} -type \"{{ target.type }}\" --hostname \"{{ target.attributes.hostname }}\"", true), monitoringDefinitionsLayerID, ciMonModuleHost, changeset, trans);
                await attributeModel.InsertAttribute("monitoring.naemon.config_template",
                    AttributeScalarValueText.Build("check_windows_host_cmd -ciid {{ target.ciid }} -type \"{{ target.type }}\" -foo --hostname \"{{ target.attributes.hostname }}\"", true), monitoringDefinitionsLayerID, ciMonModuleHostWindows, changeset, trans);
                await attributeModel.InsertAttribute("monitoring.naemon.config_template", 
                    AttributeScalarValueText.Build(
@"{{~ for related_ci in target.relations.back.runs_on ~}}
- name: service_name
  command: check_command {{ related_ci.attributes.application_name}}
{{~ end ~}}"
                        , true), monitoringDefinitionsLayerID, ciMonModuleHostLinux, changeset, trans);
                trans.Commit();
            }

            // create monitoring relations
            var windowsHosts = await ciModel.GetMergedCIs(await layerModel.BuildLayerSet(new[] { "CMDB" }, null), MultiCIIDsSelection.Build(windowsHostCIIds), true, null, TimeThreshold.BuildLatest());
            foreach (var ci in windowsHosts)
            {
                using var trans = conn.BeginTransaction();
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await relationModel.InsertRelation(ci.ID, ciMonModuleHost, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                await relationModel.InsertRelation(ci.ID, ciMonModuleHostWindows, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                trans.Commit();
            }
            var linuxHosts = await ciModel.GetMergedCIs(await layerModel.BuildLayerSet(new[] { "CMDB" }, null), MultiCIIDsSelection.Build(linuxHostCIIds), true, null, TimeThreshold.BuildLatest());
            foreach (var ci in linuxHosts)
            {
                using var trans = conn.BeginTransaction();
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await relationModel.InsertRelation(ci.ID, ciMonModuleHost, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                await relationModel.InsertRelation(ci.ID, ciMonModuleHostLinux, "has_monitoring_module", monitoringDefinitionsLayerID, changeset, trans);
                trans.Commit();
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
}
