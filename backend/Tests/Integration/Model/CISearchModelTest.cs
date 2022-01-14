﻿using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    class CISearchModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var baseAttributeModel = new BaseAttributeModel(new PartitionModel(), new CIIDModel());
            var attributeModel = new AttributeModel(baseAttributeModel);
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var relationModel = new RelationModel(new BaseRelationModel(new PartitionModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var traitsProvider = new MockedTraitsProvider();
            var traitModel = new EffectiveTraitModel(relationModel);
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            string layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await layerModel.CreateLayerIfNotExists("l1", trans);
                var (layer2, _) = await layerModel.CreateLayerIfNotExists("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertCINameAttribute("ci1", ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("ci2", ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertCINameAttribute("ci3", ciid3, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans); // name on different layer
                var i1 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var i2 = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var i3 = await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text1"), ciid1, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            var tt = TimeThreshold.BuildLatest();

            var transI = ModelContextBuilder.BuildImmediate();

            var all = await ciModel.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID1, layerID2), true, AllAttributeSelection.Instance, transI, tt);

            async Task<IEnumerable<MergedCI>> FindMergedCIsByTraits(ICIModel ciModel, IEffectiveTraitModel traitModel, IEnumerable<ITrait> withEffectiveTraits, IEnumerable<ITrait> withoutEffectiveTraits, LayerSet layerSet, IModelContext transI)
            {
                var workCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, includeEmptyCIs: true, AllAttributeSelection.Instance, transI, tt);
                return await traitModel.FilterMergedCIsByTraits(workCIs, withEffectiveTraits, withoutEffectiveTraits, new LayerSet(layerID1, layerID2), transI, tt);
            }

            var activeTraits = await traitsProvider.GetActiveTraits(transI, tt);
            (await FindMergedCIsByTraits(ciModel, traitModel, Enumerable.Empty<ITrait>(), Enumerable.Empty<ITrait>(), new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(all, options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(ciModel, traitModel, new ITrait[] { activeTraits["test_trait_3"] }, Enumerable.Empty<ITrait>(), new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(all.Where(ci => ci.CIName == "ci1"), options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(ciModel, traitModel, new ITrait[] { activeTraits["test_trait_3"] }, Enumerable.Empty<ITrait>(), new LayerSet(layerID2), transI)).Should().BeEquivalentTo(ImmutableArray<MergedCI>.Empty, options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(ciModel, traitModel, new ITrait[] { activeTraits["test_trait_4"] }, Enumerable.Empty<ITrait>(), new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(ImmutableArray<MergedCI>.Empty, options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(ciModel, traitModel, Enumerable.Empty<ITrait>(), new ITrait[] { activeTraits["test_trait_3"] }, new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(all.Where(ci => ci.CIName != "ci1"), options => options.WithStrictOrdering());

        }
    }
}
