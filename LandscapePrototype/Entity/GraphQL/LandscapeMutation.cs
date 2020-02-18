using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeMutation : ObjectGraphType
    {
        public LandscapeMutation(CIModel ciModel, LayerModel layerModel, NpgsqlConnection conn)
        {
            FieldAsync<LongGraphType>("mutate",
              arguments: new QueryArguments(
                new QueryArgument<ListGraphType<CreateLayerInputType>> { Name = "CreateLayers" },
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "CreateCIs" },
                new QueryArgument<ListGraphType<InsertCIAttributeInputType>> { Name = "InsertAttributes" }
              ),
              resolve: async context =>
              {
                  var layers = context.GetArgument<List<CreateLayerInput>>("CreateLayers", new List<CreateLayerInput>());
                  var cis = context.GetArgument<List<CreateCIInput>>("CreateCIs", new List<CreateCIInput>());
                  var attributes = context.GetArgument<List<InsertCIAttributeInput>>("InsertAttributes", new List<InsertCIAttributeInput>());

                  // TODO: add transaction to model
                  using var transaction = await conn.BeginTransactionAsync();

                  var changesetID = await ciModel.CreateChangeset();

                  foreach (var layer in layers)
                  {
                      await layerModel.CreateLayer(layer.Name);
                  }

                  foreach (var ci in cis)
                  {
                      await ciModel.CreateCI(ci.Identity); // TODO: add changeset
                  }

                  var groupedAttributes = attributes.GroupBy(a => a.CI);

                  foreach (var attributeGroup in groupedAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      var ciid = await ciModel.GetCIIDFromIdentity(ciIdentity);
                      foreach (var attribute in attributeGroup)
                      {
                          // look for layer
                          var layerID = await layerModel.GetLayerID(attribute.Layer);

                          var nonGenericAttributeValue = AttributeValueBuilder.Build(attribute.Value);

                          await ciModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, layerID, ciid, changesetID);
                      }
                  }

                  await transaction.CommitAsync();

                  return 0L;
              });
        }
    }
}
