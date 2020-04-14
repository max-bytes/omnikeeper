using Landscape.Base.Entity;
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

            await predicateModel.CreatePredicate("p1", "p1wf", "p1wt", null);
            await predicateModel.CreatePredicate("p2", "p2wf", "p2wt", null);
            await predicateModel.CreatePredicate("p3", "p3wf", "p3wt", null);
            await predicateModel.CreatePredicate("p4", "p4wf", "p4wt", null);

            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", PredicateState.Active) },
                { "p2", Predicate.Build("p2", "p2wf", "p2wt", PredicateState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", PredicateState.Active) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", PredicateState.Active) }
            }, await predicateModel.GetPredicates(null, null, Landscape.Base.Model.IPredicateModel.PredicateStateFilter.All));

            // update a wording
            Assert.IsTrue(await predicateModel.UpdateWording("p2", "p2wfn", "p2wtn", null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", PredicateState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", PredicateState.Active) }, // <- new
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", PredicateState.Active) },
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", PredicateState.Active) }
            }, await predicateModel.GetPredicates(null, null, Landscape.Base.Model.IPredicateModel.PredicateStateFilter.All));

            // update wording of non-existing predicate
            Assert.ThrowsAsync<PostgresException>(async () => await predicateModel.UpdateWording("pNonExisting", "foo", "bar", null));

            // update a state
            Assert.IsTrue(await predicateModel.UpdateState("p3", PredicateState.Inactive, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", PredicateState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", PredicateState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", PredicateState.Inactive) }, // <- new
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", PredicateState.Active) }
            }, await predicateModel.GetPredicates(null, null, Landscape.Base.Model.IPredicateModel.PredicateStateFilter.All));


            // update multiple states
            Assert.IsTrue(await predicateModel.UpdateState("p3", PredicateState.Active, null));
            Assert.IsTrue(await predicateModel.UpdateState("p4", PredicateState.Inactive, null));
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", PredicateState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", PredicateState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", PredicateState.Active) }, // <- new
                { "p4", Predicate.Build("p4", "p4wf", "p4wt", PredicateState.Inactive) }, // <- new
            }, await predicateModel.GetPredicates(null, null, Landscape.Base.Model.IPredicateModel.PredicateStateFilter.All));

            // get only active predicates
            Assert.AreEqual(new Dictionary<string, Predicate>()
            {
                { "p1", Predicate.Build("p1", "p1wf", "p1wt", PredicateState.Active) },
                { "p2", Predicate.Build("p2", "p2wfn", "p2wtn", PredicateState.Active) },
                { "p3", Predicate.Build("p3", "p3wf", "p3wt", PredicateState.Active) }
            }, await predicateModel.GetPredicates(null, null, Landscape.Base.Model.IPredicateModel.PredicateStateFilter.ActiveOnly));

        }
    }
}
