﻿using Landscape.Base.Entity;
using LandscapeRegistry;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using LandscapeRegistry.Utils;
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
using static Landscape.Base.Model.IRelationModel;

namespace Tests.Integration.Model
{
    class PredicateModelTest
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
            var predicateModel = new PredicateModel(conn);

            await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, null);
            await predicateModel.InsertOrUpdate("p2", "p2wf", "p2wt", AnchorState.Active, null);
            await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Active, null);
            await predicateModel.InsertOrUpdate("p4", "p4wf", "p4wt", AnchorState.Active, null);

            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active) },
                { "p2", Predicate.Build("p2", "p2wf", "p2wt", AnchorState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active) }
            }, await predicateModel.GetPredicates(null, null, AnchorStateFilter.All));

            // update a wording
            Assert.AreEqual(Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active), await predicateModel.InsertOrUpdate("p2", "p2wfn", "p2wtn", AnchorState.Active, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active) }, // <- new
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active) }
            }, await predicateModel.GetPredicates(null, null, AnchorStateFilter.All));

            // update a state
            Assert.AreEqual(Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Inactive), await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Inactive, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Inactive) }, // <- new
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active) }
            }, await predicateModel.GetPredicates(null, null, AnchorStateFilter.All));


            // update multiple states
            Assert.AreEqual(Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active), await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Active, null));
            Assert.AreEqual(Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Inactive), await predicateModel.InsertOrUpdate("p4", "p4wf", "p4wt", AnchorState.Inactive, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active) }, // <- new
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Inactive) }, // <- new
            }, await predicateModel.GetPredicates(null, null, AnchorStateFilter.All));

            // get only active predicates
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active) }
            }, await predicateModel.GetPredicates(null, null, AnchorStateFilter.ActiveOnly));

        }
    }
}
