﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using LandscapeRegistry.GridView;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Response;
using Omnikeeper.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Controller
{
    class GridViewControllerTest : ControllerTestBase
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            // add controller
            builder.RegisterType<GridViewController>().InstancePerLifetimeScope();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMediatR(typeof(Startup));
            builder.Populate(serviceCollection);

            //builder.RegisterType<MockedTraitsProvider>().As<ITraitsProvider>().InstancePerLifetimeScope();
        }

        [Test]
        public async Task TestBasics()
        {
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var controller = ServiceProvider.GetRequiredService<GridViewController>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();

            // set traits
            var recursiveTraits = new List<RecursiveTrait>() {
                    new RecursiveTrait("test_trait_1", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
                    {
                        new TraitAttribute("a1",
                            CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false, false)
                        )
                    }, new List<TraitAttribute>() { }, new List<TraitRelation>() { })
                };
            var t = RecursiveTraitService.FlattenRecursiveTraits(recursiveTraits);
            var tt = (IDictionary<string, ITrait>)t.ToDictionary(t => t.Key, t => (ITrait)t.Value);
            ServiceProvider.GetRequiredService<ITraitsHolder>().SetTraits(tt, DateTimeOffset.Now, NullLogger<GridViewControllerTest>.Instance);

            var userInDatabase = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());

            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            string layerID1;
            string layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
                var changeset = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), changesetModel, new DataOriginV1(DataOriginType.Manual));
                var layer1 = await layerModel.CreateLayerIfNotExists("l1", trans);
                var layer2 = await layerModel.CreateLayerIfNotExists("l2", trans);
                var layerOKConfig = await layerModel.CreateLayerIfNotExists("__okconfig", trans);
                layerID1 = layer1.layer.ID;
                layerID2 = layer2.layer.ID;
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid2, layerID2, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);

                trans.Commit();
            }

            // setup user with all permissions
            var user = new AuthenticatedInternalUser(userInDatabase);
            currentUserServiceMock.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            var cfg1 = new GridViewConfiguration(
                            true,
                            layerID1,
                            new List<string> { layerID1, layerID2 },
                            new List<GridViewColumn>()
                            {
                                new GridViewColumn("a1", null, "", layerID1, AttributeValueType.Text),
                                new GridViewColumn("a2", null, "", "invalid", AttributeValueType.Text) // invalid write layer
                            },
                            "test_trait_1"
                        );

            // add test gridview context
            var r = await controller.AddContext(new Omnikeeper.GridView.Request.AddContextRequest("ctx1", "Context 1", "Description", cfg1));
            r.Should().BeOfType<CreatedAtActionResult>();

            // test fetching single context
            var r2 = await controller.GetGridViewContext("ctx1");
            r2.Should().BeOfType<OkObjectResult>();
            var ctxData = ((r2 as OkObjectResult)!.Value as GetContextResponse);
            ctxData.Should().NotBeNull();
            ctxData!.Context.Should().BeEquivalentTo(new GridViewContext("ctx1", "Context 1", "Description", cfg1), options => options.WithStrictOrdering());

            // test getting data
            var r3 = await controller.GetData("ctx1");
            r3.Should().BeOfType<OkObjectResult>();
            var data = ((r3 as OkObjectResult)!.Value as GetDataResponse);
            data.Should().NotBeNull();
            data!.Rows.Should().BeEquivalentTo(new Row[]
            {
                new Row(
                    ciid2,
                    new List<Cell>()
                    {
                        new Cell("columnID_a1", new AttributeValueDTO() { Values = new string[] { "text1" }, IsArray = false, Type = AttributeValueType.Text }, true),
                        new Cell("columnID_a2", new AttributeValueDTO() { Values = new string[] { "text2" }, IsArray = false, Type = AttributeValueType.Text }, true),
                    }
                ),
                new Row(
                    ciid1,
                    new List<Cell>()
                    {
                        new Cell("columnID_a1", new AttributeValueDTO() { Values = new string[] { "text1" }, IsArray = false, Type = AttributeValueType.Text }, true),
                        new Cell("columnID_a2", new AttributeValueDTO() { Values = new string[] { }, IsArray = false, Type = AttributeValueType.Text }, true), // empty / not-set cell
                    }
                ),
            }, options => options.WithoutStrictOrdering());
        }
    }
}
