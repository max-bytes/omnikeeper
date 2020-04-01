using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tests.Integration;
using Tests.Integration.Model;

namespace Tests.DBInit
{
    [Explicit]
    //[Ignore("Only manual")]
    class DBInit
    {

        [Test]
        public async Task Run()
        {
            DBSetup._Setup("landscape_prototype");

            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build("landscape_prototype", false, true);

            var ciModel = new CIModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var predicateModel = new PredicateModel(conn);
            var relationModel = new RelationModel(predicateModel, conn);

            var random = new Random(3);

            var numRegularCIs = 500;
            var numRegularRelations = 5000;
            int numAttributesPerCIFrom = 20;
            int numAttributesPerCITo = 40;
            var regularTypeIDs = new[] { "Host Linux", "Host Windows", "Application" };


            var regularPredicates = new[] { 
                Predicate.Build("is_part_of", "is part of", "has part"), 
                Predicate.Build("runs_on", "runs on", "is running"),
                Predicate.Build("is_attached_to", "is attached to", "has attachment"),
            };
            var monitoringPredicates = new[] {
                Predicate.Build("has_monitoring_module", "has monitoring module", "is assigned to"),
                Predicate.Build("is_monitored_by", "is monitored by", "monitors")
            };
            var regularAttributeNames = new[] { "att_1", "att_2", "att_3", "att_4", "att_5", "att_6", "att_7", "att_8", "att_9" };
            var regularAttributeValues = Enumerable.Range(0, 10).Select<int, IAttributeValue>(i => {
                var r = random.Next(6);
                if (r == 0)
                    return AttributeValueIntegerScalar.Build(random.Next(1000));
                else if (r == 1)
                    return AttributeValueTextArray.Build(Enumerable.Range(0, random.Next(1, 5)).Select(i => $"value_{i}").ToArray());
                else
                    return AttributeValueTextScalar.Build($"attribute value {i + 1}");
            }).ToArray();

            var user = await DBSetup.SetupUser(userModel, "init-user", new Guid("3544f9a7-cc17-4cba-8052-f88656cf1ef1"));

            var regularCiids = Enumerable.Range(0, numRegularCIs).Select(i =>
            {
                var identity = "H" + RandomString.Generate(9, random, "0123456789");
                return identity;
            }).ToList();

            using (var trans = conn.BeginTransaction())
            {
                foreach (var ciType in regularTypeIDs)
                    await ciModel.CreateCIType(ciType, trans);
                await ciModel.CreateCIType("Monitoring Check Module", trans);
                await ciModel.CreateCIType("Naemon Instance", trans);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                foreach(var ciid in regularCiids)
                    await ciModel.CreateCIWithType(ciid, regularTypeIDs.GetRandom(random), trans);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                foreach (var predicate in regularPredicates.Concat(monitoringPredicates))
                    await predicateModel.CreatePredicate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, trans);

                trans.Commit();
            }

            long cmdbLayerID;
            long monitoringDefinitionsLayerID;
            using (var trans = conn.BeginTransaction())
            {
                cmdbLayerID = await layerModel.CreateLayer("CMDB", trans);
                await layerModel.CreateLayer("Inventory Scan", trans);
                monitoringDefinitionsLayerID = await layerModel.CreateLayer("Monitoring Definitions", trans);
                await layerModel.CreateLayer("Monitoring", "TestPlugin.CLBMonitoring", trans);
                trans.Commit();
            }

            // regular attributes
            foreach(var ciid in regularCiids)
            {
                var numAttributeChanges = random.Next(numAttributesPerCIFrom, numAttributesPerCITo);
                for (int i = 0; i < numAttributeChanges; i++)
                {
                    using var trans = conn.BeginTransaction();
                    var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                    var name = regularAttributeNames.GetRandom(random);
                    var value = regularAttributeValues.GetRandom(random);
                    await ciModel.InsertAttribute(name, value, cmdbLayerID, ciid, changeset.ID, trans);
                    // TODO: attribute removals
                    trans.Commit();
                }
            }

            // regular relations
            for (var i = 0;i < numRegularRelations;i++)
            {
                using var trans = conn.BeginTransaction();
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var ciid1 = regularCiids.GetRandom(random);
                var ciid2 = regularCiids.Except(new[] { ciid1 }).GetRandom(random); // TODO, HACK: slow
                var predicate = regularPredicates.GetRandom(random);
                await relationModel.InsertRelation(ciid1, ciid2, predicate.ID, cmdbLayerID, changeset.ID, trans);
                trans.Commit();
            }

            // build monitoring things
            using (var trans = conn.BeginTransaction())
            {
                await ciModel.CreateCIWithType("MON_NAEMON01", "Naemon Instance", null);
                await ciModel.CreateCIWithType("MON_NAEMON02", "Naemon Instance", null);

                await ciModel.CreateCIWithType("MON_MODULE_HOST", "Monitoring Check Module", null);
                await ciModel.CreateCIWithType("MON_MODULE_HOST_WINDOWS", "Monitoring Check Module", null);
                await ciModel.CreateCIWithType("MON_MODULE_HOST_LINUX", "Monitoring Check Module", null);
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await ciModel.InsertAttribute("monitoring.commands.check_host_cmd", AttributeValueTextScalar.Build("check_host_cmd -ciid {{ target.ciid }} -type \"{{ target.type }}\" -value \"{{ target.att_1.value }}\""), monitoringDefinitionsLayerID, "MON_MODULE_HOST", changeset.ID, trans);
                await ciModel.InsertAttribute("monitoring.commands.check_windows_host_cmd", AttributeValueTextScalar.Build("check_windows_host_cmd -ciid {{ target.ciid }} -type \"{{ target.type }}\" -foo -value \"{{ target.att_1.value }}\""), monitoringDefinitionsLayerID, "MON_MODULE_HOST_WINDOWS", changeset.ID, trans);
                await ciModel.InsertAttribute("monitoring.commands.check_linux_host_cmd", AttributeValueTextScalar.Build("check_linux_host_cmd -ciid {{ target.ciid }} -type \"{{ target.type }}\" -foo -value \"{{ target.att_1.value }}\""), monitoringDefinitionsLayerID, "MON_MODULE_HOST_LINUX", changeset.ID, trans);
                trans.Commit();
            }


            var windowsHosts = await ciModel.GetMergedCIsByType(await layerModel.BuildLayerSet(new[] { "CMDB" }, null), null, DateTimeOffset.Now, "Host Windows");
            foreach(var ci in windowsHosts)
            {
                using var trans = conn.BeginTransaction();
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST", "has_monitoring_module", monitoringDefinitionsLayerID, changeset.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST_WINDOWS", "has_monitoring_module", monitoringDefinitionsLayerID, changeset.ID, trans);
                trans.Commit();
            }
            var linuxHosts = await ciModel.GetMergedCIsByType(await layerModel.BuildLayerSet(new[] { "CMDB" }, null), null, DateTimeOffset.Now, "Host Linux");
            foreach (var ci in linuxHosts)
            {
                using var trans = conn.BeginTransaction();
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST", "has_monitoring_module", monitoringDefinitionsLayerID, changeset.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST_LINUX", "has_monitoring_module", monitoringDefinitionsLayerID, changeset.ID, trans);
                trans.Commit();
            }
        }
    }
}
