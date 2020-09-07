﻿using Landscape.Base.Entity;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

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

            await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits, null);
            await predicateModel.InsertOrUpdate("p2", "p2wf", "p2wt", AnchorState.Active, PredicateModel.DefaultConstraits, null);
            await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits, null);
            await predicateModel.InsertOrUpdate("p4", "p4wf", "p4wt", AnchorState.Active, PredicateModel.DefaultConstraits, null);

            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p2", Predicate.Build("p2", "p2wf", "p2wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active, PredicateModel.DefaultConstraits) }
            }, await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

            // update a wording
            Assert.AreEqual(Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits), await predicateModel.InsertOrUpdate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits) }, // <- new
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active, PredicateModel.DefaultConstraits) }
            }, await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

            // update a state
            Assert.AreEqual(Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Inactive, PredicateModel.DefaultConstraits), await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Inactive, PredicateModel.DefaultConstraits, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Inactive, PredicateModel.DefaultConstraits) }, // <- new
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active, PredicateModel.DefaultConstraits) }
            }, await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.All));


            // update multiple states
            Assert.AreEqual(Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits), await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits, null));
            Assert.AreEqual(Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Inactive, PredicateModel.DefaultConstraits), await predicateModel.InsertOrUpdate("p4", "p4wf", "p4wt", AnchorState.Inactive, PredicateModel.DefaultConstraits, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits) }, // <- new
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Inactive, PredicateModel.DefaultConstraits) }, // <- new
            }, await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

            // get only active predicates
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits) }
            }, await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.ActiveOnly));



            // test contraints TODO
            await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, new PredicateConstraints(new string[0], new string[] { "t1", "t2" }), null);
            var p = await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.ActiveOnly);
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, new PredicateConstraints(new string[0], new string[] { "t1", "t2" })) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits) }
            }, p);
        }

        [Test]
        public async Task TestPredicateModel()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var predicateModel = new PredicateModel(conn);

            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p2", Predicate.Build("p2", "p2wf", "p2wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", AnchorState.Active, PredicateModel.DefaultConstraits) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", AnchorState.Active, PredicateModel.DefaultConstraits) }
            }, await predicateModel.GetPredicates(null, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

        }
    }
}
