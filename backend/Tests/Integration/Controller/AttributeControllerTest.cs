using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using Npgsql;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Omnikeeper.Controllers;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Omnikeeper.Base.Entity.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Omnikeeper.Base.Service;
using Moq;

namespace Tests.Integration.Controller
{
    class AttributeControllerTest : DIServicedTestBase
    {
        protected override IServiceCollection InitServices()
        {
            var services = base.InitServices();

            // add controller
            services.AddScoped<AttributeController>();

            var lbas = new Mock<ILayerBasedAuthorizationService>();
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<AuthenticatedUser>(), It.IsAny<Layer>())).Returns(true);
            services.AddScoped((sp) => lbas.Object);
            var cbas = new Mock<ICIBasedAuthorizationService>();
            cbas.Setup(x => x.CanReadCI(It.IsAny<Guid>())).Returns(true);
            Guid? tmp;
            cbas.Setup(x => x.CanReadAllCIs(It.IsAny<IEnumerable<Guid>>(), out tmp)).Returns(true);
            services.AddScoped((sp) => cbas.Object);

            return services;
        }

        [Test]
        public async Task TestBasics()
        {
            using var scope = ServiceProvider.CreateScope();
            var conn = ServiceProvider.GetRequiredService<NpgsqlConnection>();
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var user = await DBSetup.SetupUser(userModel);
            var attributeController = ServiceProvider.GetRequiredService<AttributeController>();

            Guid ciid1;
            Guid ciid2;
            long layerID1;
            long layerID2;
            Guid attribute1ID;
            Guid attribute2ID;
            Guid changesetID;
            using (var trans = conn.BeginTransaction())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                var layer1 = await layerModel.CreateLayer("l1", trans);
                var layer2 = await layerModel.CreateLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var (attribute1, _) = await attributeModel.InsertAttribute("a1", AttributeScalarValueText.BuildFromString("text1"), ciid1, layerID1, changeset, trans);
                attribute1ID = attribute1.ID;
                changesetID = attribute1.ChangesetID;
                var (attribute2, _) = await attributeModel.InsertAttribute("a2", AttributeScalarValueText.BuildFromString("text2"), ciid2, layerID1, changeset, trans);
                attribute2ID = attribute2.ID;
                trans.Commit();
            }

            var ma1 = await attributeController.GetMergedAttribute(ciid1, "a1", new long[] { layerID1 });

            var expectedAttribute1 = CIAttributeDTO.Build(
                MergedCIAttribute.Build(
                    CIAttribute.Build(attribute1ID, "a1", ciid1, AttributeScalarValueText.BuildFromString("text1"), AttributeState.New, changesetID),
                    new long[] { layerID1 }
                ));
            (ma1.Result as OkObjectResult).Value.Should().BeEquivalentTo(expectedAttribute1);


            var ma2 = await attributeController.GetMergedAttributes(new Guid[] { ciid1, ciid2 }, new long[] { layerID1 });

            var expectedAttribute2 = CIAttributeDTO.Build(
                MergedCIAttribute.Build(
                    CIAttribute.Build(attribute2ID, "a2", ciid2, AttributeScalarValueText.BuildFromString("text2"), AttributeState.New, changesetID),
                    new long[] { layerID1 }
                ));
            var r = (ma2.Result as OkObjectResult).Value;
            r.Should().BeEquivalentTo(new CIAttributeDTO[] { expectedAttribute1, expectedAttribute2 });


            var ma3 = await attributeController.GetMergedAttributesWithName("a2", new long[] { layerID1 });
            (ma3.Result as OkObjectResult).Value.Should().BeEquivalentTo(new CIAttributeDTO[] { expectedAttribute2 });


        }
    }
}
