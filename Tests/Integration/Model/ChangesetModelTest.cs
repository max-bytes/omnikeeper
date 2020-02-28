using LandscapePrototype;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class ChangesetModelTest
    {
        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task TestBasics()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var changesetModel = new ChangesetModel(conn);
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);
            var layerModel = new LayerModel(conn);

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            var ciid3 = await ciModel.CreateCI("H789", trans);
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layerID1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layerID1 });
            var changesetID1 = await changesetModel.CreateChangeset(trans2);
            await ciModel.InsertAttribute("a1", AttributeValueText.Build("textL1"), layerID1, ciid2, changesetID1, trans2);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = conn.BeginTransaction();
            var changesetID2 = await changesetModel.CreateChangeset(trans3);
            await ciModel.InsertAttribute("a2", AttributeValueText.Build("textL1"), layerID1, ciid3, changesetID2, trans3);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = conn.BeginTransaction();
            var changesetID3 = await changesetModel.CreateChangeset(trans4);
            await ciModel.InsertAttribute("a3", AttributeValueText.Build("textL1"), layerID1, ciid3, changesetID3, trans4);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            var changesets = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, null, null);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, ciid3, null);
            Assert.AreEqual(2, changesets2.Count());
        }
    }
}
