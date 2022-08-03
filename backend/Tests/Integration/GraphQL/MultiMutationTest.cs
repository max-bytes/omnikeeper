using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class MultiMutationTest : QueryTestBase
    {
        [Test]
        public async Task TestMultiMutationSingleChangeset()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var userInDatabase = await SetupDefaultUser();
            var changeset = await CreateChangesetProxy();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", trans);
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[]
                {
                    new AuthRole("ar1", new string[] { PermissionUtils.GetLayerReadPermission(layer1), PermissionUtils.GetLayerWritePermission(layer1) }),
                });
            trans.Commit();

            await ReinitSchema();

            string query = @"
                mutation($read_layers: [String]!, $write_layer: String!, $ciid: Guid!) {
                    A : mutateCIs(writeLayer: $write_layer, readLayers: $read_layers, insertAttributes: [
                    {
                        ci: $ciid,
                        name: ""foo"",
                        value:
                        {
                            type: TEXT,
                            isArray: false,
                            values: [""bar1""]
                        }
                    }
                    ]) {
                        affectedCIs {
                            id
                        }
                    }
                    B : mutateCIs(writeLayer: $write_layer, readLayers: $read_layers, insertAttributes: [
                    {
                        ci: $ciid,
                        name: ""foo2"",
                        value:
                        {
                            type: TEXT,
                            isArray: false,
                            values: [""bar2""]
                        }
                    }
                    ]) {
                        affectedCIs {
                            id
                        }
                    }
                }";

            var inputs = new Inputs(new Dictionary<string, object?>()
                {
                    { "ciid", ciid1 },
                    { "read_layers", new string[] { "layer_1" } },
                    { "write_layer", "layer_1" }
                });

            var expected = @$"{{
                ""A"":{{""affectedCIs"":[{{""id"":""{ciid1}""}}]}},
                ""B"":{{""affectedCIs"":[{{""id"":""{ciid1}""}}]}}
            }}";

            AssertQuerySuccess(query, expected, user, inputs);

            // assert that only a single changeset was created
            using var transI = ModelContextBuilder.BuildImmediate();
            var numChangesets = await GetService<IChangesetModel>().GetNumberOfChangesets(transI);
            Assert.AreEqual(1, numChangesets);
        }
    }
}
