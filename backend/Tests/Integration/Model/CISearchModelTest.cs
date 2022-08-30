using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

namespace Tests.Integration.Model
{
    class CISearchModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                ciid2 = await GetService<ICIModel>().CreateCI(trans);
                ciid3 = await GetService<ICIModel>().CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            string layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertCINameAttribute("ci1", ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertCINameAttribute("ci2", ciid2, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertCINameAttribute("ci3", ciid3, layerID2, changeset, trans, OtherLayersValueHandlingForceWrite.Instance); // name on different layer
                var i1 = await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                var i2 = await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                var i3 = await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("text1"), ciid1, layerID2, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);

                trans.Commit();
            }

            var tt = TimeThreshold.BuildLatest();

            var transI = ModelContextBuilder.BuildImmediate();

            var all = await GetService<ICIModel>().GetMergedCIs(AllCIIDsSelection.Instance, new LayerSet(layerID1, layerID2), true, AllAttributeSelection.Instance, transI, tt);

            async Task<IEnumerable<MergedCI>> FindMergedCIsByTraits(ICIModel ciModel, IEffectiveTraitModel traitModel, IEnumerable<ITrait> withEffectiveTraits, IEnumerable<ITrait> withoutEffectiveTraits, LayerSet layerSet, IModelContext transI)
            {
                var workCIs = await ciModel.GetMergedCIs(AllCIIDsSelection.Instance, layerSet, includeEmptyCIs: true, AllAttributeSelection.Instance, transI, tt);
                return traitModel.FilterMergedCIsByTraits(workCIs, withEffectiveTraits, withoutEffectiveTraits, new LayerSet(layerID1, layerID2), transI, tt);
            }

            var traitsProvider = new MockedTraitsProvider();
            var activeTraits = await traitsProvider.GetActiveTraits(transI, tt);
            (await FindMergedCIsByTraits(GetService<ICIModel>(), GetService<IEffectiveTraitModel>(), Enumerable.Empty<ITrait>(), Enumerable.Empty<ITrait>(), new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(all, options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(GetService<ICIModel>(), GetService<IEffectiveTraitModel>(), new ITrait[] { activeTraits["test_trait_3"] }, Enumerable.Empty<ITrait>(), new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(all.Where(ci => ci.CIName == "ci1"), options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(GetService<ICIModel>(), GetService<IEffectiveTraitModel>(), new ITrait[] { activeTraits["test_trait_3"] }, Enumerable.Empty<ITrait>(), new LayerSet(layerID2), transI)).Should().BeEquivalentTo(ImmutableArray<MergedCI>.Empty, options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(GetService<ICIModel>(), GetService<IEffectiveTraitModel>(), new ITrait[] { activeTraits["test_trait_4"] }, Enumerable.Empty<ITrait>(), new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(ImmutableArray<MergedCI>.Empty, options => options.WithStrictOrdering());
            (await FindMergedCIsByTraits(GetService<ICIModel>(), GetService<IEffectiveTraitModel>(), Enumerable.Empty<ITrait>(), new ITrait[] { activeTraits["test_trait_3"] }, new LayerSet(layerID1, layerID2), transI)).Should().BeEquivalentTo(all.Where(ci => ci.CIName != "ci1"), options => options.WithStrictOrdering());

        }
    }
}
