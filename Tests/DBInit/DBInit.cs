using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
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
    [Ignore("Only manual")]
    class DBInit
    {

        [Test]
        public async Task Run()
        {
            DBSetup._Setup("landscape_prototype");

            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build("landscape_prototype", false, true);

            var ciModel = new CIModel(conn);
            var changesetModel = new ChangesetModel(conn);
            var layerModel = new LayerModel(conn);
            var userModel = new UserModel(conn);
            var relationModel = new RelationModel(conn);

            var random = new Random(3);

            var numRegularCIs = 1;
            var numRegularRelations = 0;
            int numAttributesTo = 0;
            int numAttributesFrom = 0;
        var regularTypeNames = new[] { "Host Linux", "Host Windows", "Application" };
            var regularRelationNames = new[] { "is part of", "runs on", "is attached to" };
            var regularAttributeNames = new[] { "att_1", "att_2", "att_3", "att_4", "att_5", "att_6", "att_7", "att_8", "att_9" };
            var regularAttributeValues = new[] { "foo", "bar", "blub", "bla", "this", "are", "all", "values" };

            var user = await DBSetup.SetupUser(userModel, "init-user", new Guid("3544f9a7-cc17-4cba-8052-f88656cf1ef1"));

            var regularCiids = Enumerable.Range(0, numRegularCIs).Select(i =>
            {
                var identity = "H" + RandomString.Generate(8, random, "0123456789");
                return identity;
            }).ToList();

            using (var trans = conn.BeginTransaction())
            {
                foreach(var ciid in regularCiids)
                    await ciModel.CreateCI(ciid, trans);

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
                using (var trans = conn.BeginTransaction())
                {
                    var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                    var type = regularTypeNames.GetRandom(random);
                    await ciModel.InsertAttribute("__type", AttributeValueText.Build(type), cmdbLayerID, ciid, changeset.ID, trans);
                    trans.Commit();
                }

                var numAttributeChanges = random.Next(numAttributesFrom, numAttributesTo);
                for (int i = 0; i < numAttributeChanges; i++)
                {
                    using var trans = conn.BeginTransaction();
                    var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                    var name = regularAttributeNames.GetRandom(random);
                    var value = regularAttributeValues.GetRandom(random);
                    await ciModel.InsertAttribute(name, AttributeValueText.Build(value), cmdbLayerID, ciid, changeset.ID, trans);
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
                var predicate = regularRelationNames.GetRandom(random);
                await relationModel.InsertRelation(ciid1, ciid2, predicate, cmdbLayerID, changeset.ID, trans);
                trans.Commit();
            }

            // build monitoring things
            using (var trans = conn.BeginTransaction())
            {
                await ciModel.CreateCI("MON_MODULE_HOST", null);
                await ciModel.CreateCI("MON_MODULE_HOST_WINDOWS", null);
                await ciModel.CreateCI("MON_MODULE_HOST_LINUX", null);
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await ciModel.InsertAttribute("__type", AttributeValueText.Build("Monitoring Check Module"), monitoringDefinitionsLayerID, "MON_MODULE_HOST", changeset.ID, trans);
                await ciModel.InsertAttribute("__type", AttributeValueText.Build("Monitoring Check Module"), monitoringDefinitionsLayerID, "MON_MODULE_HOST_WINDOWS", changeset.ID, trans);
                await ciModel.InsertAttribute("__type", AttributeValueText.Build("Monitoring Check Module"), monitoringDefinitionsLayerID, "MON_MODULE_HOST_LINUX", changeset.ID, trans);
                await ciModel.InsertAttribute("monitoring.commands.check_host_cmd", AttributeValueText.Build("check_host_cmd"), monitoringDefinitionsLayerID, "MON_MODULE_HOST", changeset.ID, trans);
                await ciModel.InsertAttribute("monitoring.commands.check_windows_host_cmd", AttributeValueText.Build("check_windows_host_cmd"), monitoringDefinitionsLayerID, "MON_MODULE_HOST_WINDOWS", changeset.ID, trans);
                await ciModel.InsertAttribute("monitoring.commands.check_linux_host_cmd", AttributeValueText.Build("check_linux_host_cmd"), monitoringDefinitionsLayerID, "MON_MODULE_HOST_LINUX", changeset.ID, trans);
                trans.Commit();
            }


            var windowsHosts = await ciModel.GetCIsWithType(await layerModel.BuildLayerSet(new[] { "CMDB" }, null), null, DateTimeOffset.Now, "Host Windows");
            foreach(var ci in windowsHosts)
            {
                using var trans = conn.BeginTransaction();
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST", "is monitored via", monitoringDefinitionsLayerID, changeset.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST_WINDOWS", "is monitored via", monitoringDefinitionsLayerID, changeset.ID, trans);
                trans.Commit();
            }
            var linuxHosts = await ciModel.GetCIsWithType(await layerModel.BuildLayerSet(new[] { "CMDB" }, null), null, DateTimeOffset.Now, "Host Linux");
            foreach (var ci in linuxHosts)
            {
                using var trans = conn.BeginTransaction();
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST", "is monitored via", monitoringDefinitionsLayerID, changeset.ID, trans);
                await relationModel.InsertRelation(ci.Identity, "MON_MODULE_HOST_LINUX", "is monitored via", monitoringDefinitionsLayerID, changeset.ID, trans);
                trans.Commit();
            }
        }
    }
}
