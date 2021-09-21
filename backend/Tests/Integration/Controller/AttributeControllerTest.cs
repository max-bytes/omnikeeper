using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Controllers;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Threading.Tasks;

namespace Tests.Integration.Controller
{
    class AttributeControllerTest : ControllerTestBase
    {
        protected override void InitServices(IServiceCollection services)
        {
            base.InitServices(services);

            // add controller
            services.AddScoped<AttributeController>();
        }

        [Test]
        public async Task TestBasics()
        {
            using var scope = ServiceProvider.CreateScope();
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            var attributeController = ServiceProvider.GetRequiredService<AttributeController>();

            Guid ciid1;
            Guid ciid2;
            string layerID1;
            string layerID2;
            Guid attribute1ID;
            Guid attribute2ID;
            Guid changesetID;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                var layer1 = await layerModel.UpsertLayer("l1", trans);
                var layer2 = await layerModel.UpsertLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var (attribute1, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                attribute1ID = attribute1.ID;
                changesetID = attribute1.ChangesetID;
                var (attribute2, _) = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                attribute2ID = attribute2.ID;
                trans.Commit();
            }

            var ma2 = await attributeController.GetMergedAttributes(new Guid[] { ciid1, ciid2 }, new string[] { layerID1 });

            var expectedAttribute1 = CIAttributeDTO.Build(
                new MergedCIAttribute(
                    new CIAttribute(attribute1ID, "a1", ciid1, new AttributeScalarValueText("text1"), AttributeState.New, changesetID),
                    new string[] { layerID1 }
                ));
            var expectedAttribute2 = CIAttributeDTO.Build(
                new MergedCIAttribute(
                    new CIAttribute(attribute2ID, "a2", ciid2, new AttributeScalarValueText("text2"), AttributeState.New, changesetID),
                    new string[] { layerID1 }
                ));
            var r = (ma2.Result as OkObjectResult)!.Value;
            r.Should().BeEquivalentTo(new CIAttributeDTO[] { expectedAttribute1, expectedAttribute2 }, options => options.WithStrictOrdering());


            var ma3 = await attributeController.GetMergedAttributesWithName("a2", new string[] { layerID1 });
            (ma3.Result as OkObjectResult)!.Value.Should().BeEquivalentTo(new CIAttributeDTO[] { expectedAttribute2 }, options => options.WithStrictOrdering());


        }
    }
}
