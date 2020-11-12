﻿using GraphQL;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL;
using Omnikeeper.Model;
using Omnikeeper.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Startup;
using Omnikeeper.Model.Config;
using GraphQL.Server;
using Tests.Integration.GraphQL.Base;
using Omnikeeper.Base.Service;

namespace Tests.Integration.GraphQL
{
    class CITest : QueryTestBase
    {
        protected override IServiceCollection InitServices()
        {
            var services = base.InitServices();

            var cbas = new Mock<ICIBasedAuthorizationService>();
            cbas.Setup(x => x.CanReadCI(It.IsAny<Guid>())).Returns(true);
            Guid? tmp;
            cbas.Setup(x => x.CanReadAllCIs(It.IsAny<IEnumerable<Guid>>(), out tmp)).Returns(true);
            services.AddScoped((sp) => cbas.Object);

            return services;
        }

        [Test]
        public async Task TestBasicQuery()
        {
            using var scope = ServiceProvider.CreateScope();
            var username = "testUser";
            var userGUID = new Guid("7dc848b7-881d-4785-9f25-985e9b6f2715");
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await ciModel.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("layer_1", trans);
            var layerID2 = await layerModel.CreateLayer("layer_2", trans);
            var user = new AuthenticatedUser(await userModel.UpsertUser(username, username, userGUID, UserType.Robot, trans), new List<Layer>());
            var changeset = new ChangesetProxy(user.InDatabase, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueInteger(3), ciid1, layer1.ID, changeset, trans);
            trans.Commit();

            string query = @"
                    query text($ciid: Guid!, $layers: [String]!) {
                      ci(ciid: $ciid, layers: $layers) {
                        mergedAttributes {
                            attribute {
                                name
                                state
                                value {
                                    type
                                    isArray
                                    values
                                }
                            }
                        }
                    }
                }
                ";

            var inputs = new Inputs(new Dictionary<string, object>()
                {
                    { "ciid", ciid1 },
                    { "layers", new string[] { "layer_1", "layer_2" } }
                });

            var expected = @"{
                      ""ci"":{
                         ""mergedAttributes"":[
                            {
                                ""attribute"": {
                                   ""name"":""a1"",
                                   ""state"":""NEW"",
                                   ""value"":{
                                      ""type"":""INTEGER"",
                                      ""isArray"": false,
                                      ""values"":[""3""]
                                   }
                                }
                            }
                         ]
                      }
                    }";

            var httpContext = new DefaultHttpContext();
            AssertQuerySuccess(query, expected, inputs, userContext: new OmnikeeperUserContext(user));
        }
    }
}
