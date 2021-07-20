//using Microsoft.Extensions.Logging.Abstractions;
//using NUnit.Framework;
//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Model;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Tests.Integration.Model
//{
//    class PredicateModelTest : DBBackedTestBase
//    {
//        [Test]
//        public async Task TestBasics()
//        {
//            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
//            var attributeModel = new AttributeModel(baseAttributeModel);
//            var baseRelationModel = new BaseRelationModel(new PartitionModel());
//            var relationModel = new RelationModel(baseRelationModel);
//            var ciModel = new CIModel(attributeModel, new CIIDModel());
//            var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
//            var effectiveTraitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, null, NullLogger<EffectiveTraitModel>.Instance);
//            var predicateModel = new PredicateModel(baseConfigurationModel, effectiveTraitModel);
//            var trans = ModelContextBuilder.BuildImmediate();

//            await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default, trans);
//            await predicateModel.InsertOrUpdate("p2", "p2wf", "p2wt", AnchorState.Active, PredicateConstraints.Default, trans);
//            await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default, trans);
//            await predicateModel.InsertOrUpdate("p4", "p4wf", "p4wt", AnchorState.Active, PredicateConstraints.Default, trans);

//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p1", new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p2", new Predicate("p2", "p2wf", "p2wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p3", new Predicate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p4", new Predicate("p4", "p4wf", "p4wt", AnchorState.Active, PredicateConstraints.Default) }
//            }, await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

//            // update a wording
//            Assert.AreEqual((new Predicate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default), true), await predicateModel.InsertOrUpdate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default, trans));
//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p1", new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p2", new Predicate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default) }, // <- new
//                { "p3", new Predicate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p4", new Predicate("p4", "p4wf", "p4wt", AnchorState.Active, PredicateConstraints.Default) }
//            }, await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

//            // update a state
//            Assert.AreEqual((new Predicate("p3", "p3wf", "p3wt", AnchorState.Inactive, PredicateConstraints.Default), true), await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Inactive, PredicateConstraints.Default, trans));
//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p1", new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p2", new Predicate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default) },
//                { "p3", new Predicate("p3", "p3wf", "p3wt", AnchorState.Inactive, PredicateConstraints.Default) }, // <- new
//                { "p4", new Predicate("p4", "p4wf", "p4wt", AnchorState.Active, PredicateConstraints.Default) }
//            }, await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.All));


//            // update multiple states
//            Assert.AreEqual((new Predicate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default), true), await predicateModel.InsertOrUpdate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default, trans));
//            Assert.AreEqual((new Predicate("p4", "p4wf", "p4wt", AnchorState.Inactive, PredicateConstraints.Default), true), await predicateModel.InsertOrUpdate("p4", "p4wf", "p4wt", AnchorState.Inactive, PredicateConstraints.Default, trans));
//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p1", new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p2", new Predicate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default) },
//                { "p3", new Predicate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default) }, // <- new
//                { "p4", new Predicate("p4", "p4wf", "p4wt", AnchorState.Inactive, PredicateConstraints.Default) }, // <- new
//            }, await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

//            // get only active predicates
//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p1", new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default) },
//                { "p2", new Predicate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default) },
//                { "p3", new Predicate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default) }
//            }, await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.ActiveOnly));



//            // test contraints TODO
//            await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, new PredicateConstraints(new string[0], new string[] { "t1", "t2" }), trans);
//            var p = await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.ActiveOnly);
//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p1", new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, new PredicateConstraints(new string[0], new string[] { "t1", "t2" })) },
//                { "p2", new Predicate("p2", "p2wfn", "p2wtn", AnchorState.Active, PredicateConstraints.Default) },
//                { "p3", new Predicate("p3", "p3wf", "p3wt", AnchorState.Active, PredicateConstraints.Default) }
//            }, p);
//        }

//        [Test]
//        public async Task TestGetPredicatesAtDifferentTimes()
//        {
//            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
//            var attributeModel = new AttributeModel(baseAttributeModel);
//            var baseRelationModel = new BaseRelationModel(new PartitionModel());
//            var relationModel = new RelationModel(baseRelationModel);
//            var ciModel = new CIModel(attributeModel, new CIIDModel());
//            var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
//            var effectiveTraitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, null, NullLogger<EffectiveTraitModel>.Instance);
//            var predicateModel = new PredicateModel(baseConfigurationModel, effectiveTraitModel);
//            var trans = ModelContextBuilder.BuildImmediate();

//            var now = DateTimeOffset.Now;

//            await predicateModel.InsertOrUpdate("p11", "p1wf", "p1wt", AnchorState.Inactive, PredicateConstraints.Default, trans, now);
//            await predicateModel.InsertOrUpdate("p22", "p2wf", "p2wt", AnchorState.Inactive, PredicateConstraints.Default, trans, now);

//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p11", new Predicate("p11", "p1wf", "p1wt", AnchorState.Inactive, PredicateConstraints.Default) },
//                { "p22", new Predicate("p22", "p2wf", "p2wt", AnchorState.Inactive, PredicateConstraints.Default) }
//            },
//            await predicateModel.GetPredicates(trans, TimeThreshold.BuildLatest(), AnchorStateFilter.All));

//            await predicateModel.InsertOrUpdate("p11", "test", "test", AnchorState.Active, PredicateConstraints.Default, trans, now.AddHours(-1));
//            await predicateModel.InsertOrUpdate("p22", "p2wf_1", "p2wt_1", AnchorState.Active, PredicateConstraints.Default, trans, now.AddHours(-1));

//            Assert.AreEqual(new Dictionary<string, Predicate>()
//            {
//                { "p11", new Predicate("p11", "test", "test", AnchorState.Active, PredicateConstraints.Default) },
//                { "p22", new Predicate("p22", "p2wf_1", "p2wt_1", AnchorState.Active, PredicateConstraints.Default) }
//            },
//            await predicateModel.GetPredicates(trans, TimeThreshold.BuildAtTime(now.AddHours(-1)), AnchorStateFilter.All));
//        }

//        [Test]
//        public async Task TestGetPredicate()
//        {
//            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
//            var attributeModel = new AttributeModel(baseAttributeModel);
//            var baseRelationModel = new BaseRelationModel(new PartitionModel());
//            var relationModel = new RelationModel(baseRelationModel);
//            var ciModel = new CIModel(attributeModel, new CIIDModel());
//            var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
//            var effectiveTraitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, null, NullLogger<EffectiveTraitModel>.Instance);
//            var predicateModel = new PredicateModel(baseConfigurationModel, effectiveTraitModel);
//            var trans = ModelContextBuilder.BuildImmediate();

//            await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, new PredicateConstraints(new string[0], new string[] { "t1", "t2" }), trans);

//            var predicate = new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, new PredicateConstraints(new string[0], new string[] { "t1", "t2" }));

//            Assert.AreEqual(predicate, await predicateModel.GetPredicate("p1", TimeThreshold.BuildLatest(), AnchorStateFilter.ActiveOnly, trans));
//        }

//        [Test]
//        public async Task TestPredicateChange()
//        {
//            var baseAttributeModel = new BaseAttributeModel(new PartitionModel());
//            var attributeModel = new AttributeModel(baseAttributeModel);
//            var baseRelationModel = new BaseRelationModel(new PartitionModel());
//            var relationModel = new RelationModel(baseRelationModel);
//            var ciModel = new CIModel(attributeModel, new CIIDModel());
//            var baseConfigurationModel = new BaseConfigurationModel(NullLogger<BaseConfigurationModel>.Instance);
//            var effectiveTraitModel = new EffectiveTraitModel(ciModel, attributeModel, relationModel, null, NullLogger<EffectiveTraitModel>.Instance);
//            var predicateModel = new PredicateModel(baseConfigurationModel, effectiveTraitModel);
//            var trans = ModelContextBuilder.BuildImmediate();

//            // should return changed true since predicate is inserting
//            Assert.AreEqual((new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default), true),
//                  await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default, trans));

//            // should return changed false since nothing has changed
//            Assert.AreEqual((new Predicate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default), false),
//                  await predicateModel.InsertOrUpdate("p1", "p1wf", "p1wt", AnchorState.Active, PredicateConstraints.Default, trans));

//            // should return changed true since predicate has changed
//            Assert.AreEqual((new Predicate("p1", "p1wf_u", "p1wt_u", AnchorState.Active, PredicateConstraints.Default), true),
//                  await predicateModel.InsertOrUpdate("p1", "p1wf_u", "p1wt_u", AnchorState.Active, PredicateConstraints.Default, trans));
//        }
//    }
//}
