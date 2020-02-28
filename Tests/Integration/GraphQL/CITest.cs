﻿using GraphQL;
using GraphQL.Execution;
using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Entity.GraphQL;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration.GraphQL
{
    class CITest : QueryTestBase<LandscapeSchema>
    {
        public CITest()
        {
            DBSetup.Setup();

            Services.Register<LandscapeQuery>();
            Services.Register<CIType>();
            Services.Register<CIModel>();
            Services.Register<LayerModel>();
            Services.Register<RelationModel>();
            Services.Register<RelatedCIType>();
            Services.Register<RelationType>();
            Services.Register<ChangesetModel>();
            Services.Singleton(() =>
            {
                var dbcb = new DBConnectionBuilder();
                return dbcb.Build(DBSetup.dbName, false);
            });

            Services.Singleton(new LandscapeSchema(new SimpleContainerAdapter(Services)));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Services.Dispose();
        }

        [Test]
        public async Task Test()
        {
            var ciModel = Services.Get<CIModel>();
            var layerModel = Services.Get<LayerModel>();
            var changesetModel = Services.Get<ChangesetModel>();
            using var trans = Services.Get<NpgsqlConnection>().BeginTransaction();
            var ciid1 = await ciModel.CreateCI("H123", trans);
            var layerID1 = await layerModel.CreateLayer("layer_1", trans);
            var layerID2 = await layerModel.CreateLayer("layer_2", trans);
            var changesetID = await changesetModel.CreateChangeset(trans);
            await ciModel.InsertAttribute("a1", AttributeValueInteger.Build(3), layerID1, ciid1, changesetID, trans);
            trans.Commit();

            string query = @"
                query text($ciIdentity: String!, $layers: [String]!) {
                  ci(identity: $ciIdentity, layers: $layers) {
                    attributes {
                        name
                        state
                        layerID
                        value {
                            __typename
                            ... on AttributeValueIntegerType {
                                valueInteger: value
                            }
                            ... on AttributeValueTextType {
                                valueText: value
                            }
                        }
                    }
                }
            }
            ";

            var inputs = new Inputs(new Dictionary<string, object>()
            {
                { "ciIdentity", "H123" },
                { "layers", new string[] { "layer_1", "layer_2" } }
            });

            var expected = @"{
                  ""ci"":{
                     ""attributes"":[
                        {
                           ""name"":""a1"",
                           ""state"":""NEW"",
                           ""layerID"":1,
                           ""value"":{
                              ""__typename"":""AttributeValueIntegerType"",
                              ""valueInteger"":3
                           }
                        }
                     ]
                  }
                }";

            AssertQuerySuccess(query, expected, inputs, userContext: new LandscapeUserContext());
        }
    }
}
