using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Omnikeeper.Controllers;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Omnikeeper.Base.Entity.DTO;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Utils.ModelContext;
using LandscapeRegistry.GridView;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using Omnikeeper.Base.Service;
using System.Collections.Immutable;
using MediatR;
using System.Reflection;
using Omnikeeper.Startup;
using Omnikeeper.GridView.Response;
using Omnikeeper.GridView.Entity;
using Omnikeeper.Base.Entity.DataOrigin;

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
                var changeset = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);
                var (attribute1, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (attribute2, _) = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (attribute3, _) = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid2, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (attribute4, _) = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text2"), ciid3, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            var cfg1 = new GridViewConfiguration()
            {
                Trait = "test_trait_1",
                ReadLayerset = new List<long> { layerID1, layerID2 },
                WriteLayer = layerID1,
                ShowCIIDColumn = true,
                Columns = new List<GridViewColumn>()
                    {
                        new GridViewColumn()
                        {
                            WriteLayer = layerID1,
                            ColumnDescription = "",
                            SourceAttributeName = "a1"
                        },
                        new GridViewColumn()
                        {
                            WriteLayer = 3, // invalid write layer
                            ColumnDescription = "",
                            SourceAttributeName = "a2"
                        }
                    }
            };

            // add test gridview context
            var r = await controller.AddContext(new Omnikeeper.GridView.Request.AddContextRequest()
            {
                Name = "ctx1",
                SpeakingName = "Context 1",
                Description = "Description",
                Configuration = cfg1
            });
            r.Should().BeOfType<CreatedAtActionResult>();

            // test fetching single context
            var r2 = await controller.GetContext("ctx1");
            r2.Should().BeOfType<OkObjectResult>();
            var ctxData = ((r2 as OkObjectResult)!.Value as GetContextResponse);
            ctxData.Should().NotBeNull();
            ctxData!.Context.Should().BeEquivalentTo(new FullContext()
            {
                Name = "ctx1",
                SpeakingName = "Context 1",
                Description = "Description",
                Configuration = cfg1
            });

            // test getting data
            var r3 = await controller.GetData("ctx1");
            r3.Should().BeOfType<OkObjectResult>();
            var data = ((r3 as OkObjectResult)!.Value as GetDataResponse);
            data.Should().NotBeNull();
            data!.Rows.Should().BeEquivalentTo(new Row[]
            {
                new Row() {
                    Ciid = ciid1,
                    Cells = new List<Cell>()
                    {
                        new Cell() {Name = "a1", Value = "text1", Changeable = true},
                    }
                },
                new Row() {
                    Ciid = ciid2,
                    Cells = new List<Cell>()
                    {
                        new Cell() {Name = "a1", Value = "text1", Changeable = true},
                        new Cell() {Name = "a2", Value = "text2", Changeable = true},
                    }
                }
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
