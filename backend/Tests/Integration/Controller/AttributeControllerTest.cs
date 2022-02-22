//using Autofac;
//using FluentAssertions;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.DependencyInjection;
//using NUnit.Framework;
//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Entity.DataOrigin;
//using Omnikeeper.Base.Entity.DTO;
//using Omnikeeper.Base.Model;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Base.Utils.ModelContext;
//using Omnikeeper.Controllers;
//using Omnikeeper.Entity.AttributeValues;
//using System;
//using System.Threading.Tasks;

//namespace Tests.Integration.Controller
//{
//    class AttributeControllerTest : ControllerTestBase
//    {
//        protected override void InitServices(ContainerBuilder builder)
//        {
//            base.InitServices(builder);

//            // add controller
//            builder.RegisterType<AttributeController>().InstancePerLifetimeScope();
//        }

//        [Test]
//        public async Task TestBasics()
//        {
//            var attributeController = ServiceProvider.GetRequiredService<AttributeController>();

//            Guid ciid1;
//            Guid ciid2;
//            string layerID1;
//            string layerID2;
//            Guid attribute1ID;
//            Guid attribute2ID;
//            Guid changesetID;
//            using (var trans = ModelContextBuilder.BuildDeferred())
//            {
//                ciid1 = await GetService<ICIModel>().CreateCI(trans);
//                ciid2 = await GetService<ICIModel>().CreateCI(trans);
//                var changeset = await CreateChangesetProxy();
//                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
//                var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);
//                layerID1 = layer1.ID;
//                layerID2 = layer2.ID;
//                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
//                attribute1ID = attribute1.ID;
//                changesetID = attribute1.ChangesetID;
//                var (attribute2, _) = await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
//                attribute2ID = attribute2.ID;
//                trans.Commit();
//            }

//            var ma2 = await attributeController.GetMergedAttributes(new Guid[] { ciid1, ciid2 }, new string[] { layerID1 });

//            var expectedAttribute1 = CIAttributeDTO.Build(
//                new MergedCIAttribute(
//                    new CIAttribute(attribute1ID, "a1", ciid1, new AttributeScalarValueText("text1"), changesetID),
//                    new string[] { layerID1 }
//                ));
//            var expectedAttribute2 = CIAttributeDTO.Build(
//                new MergedCIAttribute(
//                    new CIAttribute(attribute2ID, "a2", ciid2, new AttributeScalarValueText("text2"), changesetID),
//                    new string[] { layerID1 }
//                ));
//            var r = (ma2.Result as OkObjectResult)!.Value;
//            r.Should().BeEquivalentTo(new CIAttributeDTO[] { expectedAttribute1, expectedAttribute2 }, options => options.WithStrictOrdering());


//            var ma3 = await attributeController.GetMergedAttributesWithName("a2", new string[] { layerID1 });
//            (ma3.Result as OkObjectResult)!.Value.Should().BeEquivalentTo(new CIAttributeDTO[] { expectedAttribute2 }, options => options.WithStrictOrdering());


//        }
//    }
//}
