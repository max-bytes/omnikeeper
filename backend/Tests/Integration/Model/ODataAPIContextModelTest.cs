using Autofac;
using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model.Config;
using System;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class ODataAPIContextModelTest : GenericTraitEntityModelTestBase<ODataAPIContext, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<ODataAPIContextModel>().As<GenericTraitEntityModel<ODataAPIContext, string>>();
        }

        [Test]
        public async Task TestReadingV3Config()
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            // manually insert with V3 config
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = changesetBuilder();
                var configV3Attribute = new ConfigSerializerV3().SerializeToAttributeValue(new ODataAPIContext.ConfigV3("wl1", new string[] { "rl1", "rl2" }), false);
                var ciid = await GetService<ICIModel>().CreateCI(trans);
                await GetService<IAttributeModel>().InsertAttribute("odata_context.id", new AttributeScalarValueText("id1"), ciid, layer1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("odata_context.config", configV3Attribute, ciid, layer1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("__name", new AttributeScalarValueText("OData-Context - id1"), ciid, layer1, changeset, new DataOriginV1(DataOriginType.Manual), trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // should come out as configV4
            var byDataID1 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(new ODataAPIContext("id1", new ODataAPIContext.ConfigV4("wl1", new string[] { "rl1", "rl2" }, new ODataAPIContext.ContextAuthBasic("u1", "ph1"))));
        }


        [Test]
        public async Task TestWritingV3Config()
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            // inserting with V3 config must fail
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.ThrowsAsync<Exception>(async () =>
                {
                    await model.InsertOrUpdate(new ODataAPIContext("id1", new ODataAPIContext.ConfigV3("wl1", new string[] { "rl1", "rl2" })),
                    layerset, layer1,
                    new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                });
            }
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new ODataAPIContext("id1", new ODataAPIContext.ConfigV4("wl1", new string[] {"rl1", "rl2"}, new ODataAPIContext.ContextAuthBasic("u1", "ph1"))),
                () => new ODataAPIContext("id2", new ODataAPIContext.ConfigV4("wl2", new string[] { "rl3", "rl4" }, new ODataAPIContext.ContextAuthBasic("u2", "ph2"))),
                    "id1", "id2", "non_existant_id"
                );
        }

        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new ODataAPIContext("id1", new ODataAPIContext.ConfigV4("wl1", new string[] { "rl1", "rl2" }, new ODataAPIContext.ContextAuthBasic("u1", "ph1"))),
                () => new ODataAPIContext("id2", new ODataAPIContext.ConfigV4("wl2", new string[] { "rl3", "rl4" }, new ODataAPIContext.ContextAuthBasic("u2", "ph2"))),
                "id1", "id2", "non_existant_id"
            );
        }
    }
}
