﻿using LandscapePrototype;
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
            var user = await DBSetup.SetupUser(new UserModel(conn));

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            var ciid3 = await ciModel.CreateCI("H789", trans);
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layerID1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layerID1 });
            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans2);
            await ciModel.InsertAttribute("a1", AttributeValueText.Build("textL1"), layerID1, ciid2, changeset1.ID, trans2);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = conn.BeginTransaction();
            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans3);
            await ciModel.InsertAttribute("a2", AttributeValueText.Build("textL1"), layerID1, ciid3, changeset2.ID, trans3);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = conn.BeginTransaction();
            var changeset3 = await changesetModel.CreateChangeset(user.ID, trans4);
            await ciModel.InsertAttribute("a3", AttributeValueText.Build("textL1"), layerID1, ciid3, changeset3.ID, trans4);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            var changesets = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, RelationModel.IncludeRelationDirections.Forward, null, null);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, RelationModel.IncludeRelationDirections.Forward, ciid3, null);
            Assert.AreEqual(2, changesets2.Count());
        }



        [Test]
        public async Task TestRelations()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var changesetModel = new ChangesetModel(conn);
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(new UserModel(conn));

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layerID1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layerID1 });
            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans2);
            await relationModel.InsertRelation("H123", "H456", "relation_1", layerID1, changeset1.ID, trans2);
            trans2.Commit();

            Thread.Sleep(500);
            var t2 = DateTimeOffset.Now;

            using var trans3 = conn.BeginTransaction();
            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans3);
            await relationModel.InsertRelation("H456", "H123", "relation_2", layerID1, changeset2.ID, trans3);
            trans3.Commit();

            Thread.Sleep(500);
            var t3 = DateTimeOffset.Now;

            var changesets1 = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, RelationModel.IncludeRelationDirections.Forward, ciid1, null);
            Assert.AreEqual(1, changesets1.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, RelationModel.IncludeRelationDirections.Forward, ciid1, null);
            Assert.AreEqual(1, changesets2.Count()); // must still be 1, as incoming relations are not counted
        }
    }
}
