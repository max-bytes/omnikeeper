﻿using FluentAssertions;
using LandscapeRegistry.GridView;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Response;
using Omnikeeper.Startup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Tests.Integration.Controller
{
    class GridViewControllerTest : ControllerTestBase
    {
        protected override IServiceCollection InitServices()
        {
            var services = base.InitServices();

            // add controller
            services.AddScoped<GridViewController>();
            services.AddMediatR(typeof(Startup));

            services.AddScoped<ITraitsProvider, MockedTraitsProvider>();

            return services;
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
            var controller = ServiceProvider.GetRequiredService<GridViewController>();

            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            long layerID1;
            long layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
                var layer1 = await layerModel.CreateLayer("l1", trans);
                var layer2 = await layerModel.CreateLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var (attribute1, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (attribute2, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (attribute3, _) = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid2, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (attribute4, _) = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            var cfg1 = new GridViewConfiguration(
                            true,
                            layerID1,
                            new List<long> { layerID1, layerID2 },
                            new List<GridViewColumn>()
                            {
                                new GridViewColumn("a1", "", layerID1, AttributeValueType.Text),
                                new GridViewColumn("a2", "", 3, AttributeValueType.Text) // invalid write layer
                            },
                            "test_trait_1"
                        );

            // add test gridview context
            var r = await controller.AddContext(new Omnikeeper.GridView.Request.AddContextRequest("ctx1", "Context 1", "Description", cfg1));
            r.Should().BeOfType<CreatedAtActionResult>();

            // test fetching single context
            var r2 = await controller.GetContext("ctx1");
            r2.Should().BeOfType<OkObjectResult>();
            var ctxData = ((r2 as OkObjectResult)!.Value as GetContextResponse);
            ctxData.Should().NotBeNull();
            ctxData!.Context.Should().BeEquivalentTo(new FullContext("ctx1", "Context 1", "Description", cfg1));

            // test getting data
            var r3 = await controller.GetData("ctx1");
            r3.Should().BeOfType<OkObjectResult>();
            var data = ((r3 as OkObjectResult)!.Value as GetDataResponse);
            data.Should().NotBeNull();
            data!.Rows.Should().BeEquivalentTo(new Row[]
            {
                new Row(
                    ciid1,
                    new List<Cell>()
                    {
                        new Cell("a1", new AttributeValueDTO() { Values = new string[] { "text1" }, IsArray = false, Type = AttributeValueType.Text }, true),
                        new Cell("a2", new AttributeValueDTO() { Values = new string[] { }, IsArray = false, Type = AttributeValueType.Text }, true), // empty / not-set cell
                    }
                ),
                new Row(
                    ciid2,
                    new List<Cell>()
                    {
                        new Cell("a1", new AttributeValueDTO() { Values = new string[] { "text1" }, IsArray = false, Type = AttributeValueType.Text }, true),
                        new Cell("a2", new AttributeValueDTO() { Values = new string[] { "text2" }, IsArray = false, Type = AttributeValueType.Text }, true),
                    }
                )
            });
        }

        public class MockedTraitsProvider : ITraitsProvider
        {
            public async Task<Trait?> GetActiveTrait(string traitName, IModelContext trans, TimeThreshold timeThreshold)
            {
                var ts = await GetActiveTraitSet(trans, timeThreshold);

                if (ts.Traits.TryGetValue(traitName, out var trait))
                    return trait;
                return null;
            }

            public Task<TraitSet> GetActiveTraitSet(IModelContext trans, TimeThreshold timeThreshold)
            {
                var r = new List<RecursiveTrait>() {
                new RecursiveTrait("test_trait_1", new List<TraitAttribute>()
                {
                    new TraitAttribute("a1",
                        CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { })
            };

                // TODO: should we really flatten here in a mocked class?
                return Task.FromResult(TraitSet.Build(RecursiveTraitService.FlattenDependentTraits(r.ToImmutableDictionary(r => r.Name))));
            }
        }
    }
}