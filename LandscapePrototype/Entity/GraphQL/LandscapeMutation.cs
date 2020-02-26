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
        public LandscapeMutation(CIModel ciModel, LayerModel layerModel, RelationModel relationModel, NpgsqlConnection conn)
        {
            FieldAsync<MutateReturnType>("mutate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                new QueryArgument<ListGraphType<CreateLayerInputType>> { Name = "CreateLayers" },
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "CreateCIs" },
                new QueryArgument<ListGraphType<InsertCIAttributeInputType>> { Name = "InsertAttributes" },
                new QueryArgument<ListGraphType<RemoveCIAttributeInputType>> { Name = "RemoveAttributes" },
                new QueryArgument<ListGraphType<InsertRelationInputType>> { Name = "InsertRelations" }
              ),
              resolve: async context =>
              {
                  var layers = context.GetArgument<string[]>("layers");
                  var createLayers = context.GetArgument("CreateLayers", new List<CreateLayerInput>());
                  var createCIs = context.GetArgument("CreateCIs", new List<CreateCIInput>());
                  var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>());
                  var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>());
                  var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>());

                  // TODO: add transaction to model
                  using var transaction = await conn.BeginTransactionAsync();

                  var changesetID = await ciModel.CreateChangeset(transaction);

                  foreach (var layer in createLayers)
                  {
                      await layerModel.CreateLayer(layer.Name, transaction);
                  }

                  var createdCIIDs = new List<long>();
                  foreach (var ci in createCIs)
                  {
                      createdCIIDs.Add(await ciModel.CreateCI(ci.Identity, transaction)); // TODO: add changeset
                  }

                  var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                  var insertedAttributes = new List<CIAttribute>();
                  foreach (var attributeGroup in groupedInsertAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      var ciid = await ciModel.GetCIIDFromIdentity(ciIdentity, transaction);
                      foreach (var attribute in attributeGroup)
                      {
                          var nonGenericAttributeValue = AttributeValueBuilder.Build(attribute.Value);

                          insertedAttributes.Add(await ciModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, attribute.LayerID, ciid, changesetID, transaction));
                      }
                  }

                  var groupedRemoveAttributes = removeAttributes.GroupBy(a => a.CI);
                  var removedAttributes = new List<CIAttribute>();
                  foreach (var attributeGroup in groupedRemoveAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      var ciid = await ciModel.GetCIIDFromIdentity(ciIdentity, transaction);
                      foreach (var attribute in attributeGroup) {
                          removedAttributes.Add(await ciModel.RemoveAttribute(attribute.Name, attribute.LayerID, ciid, changesetID, transaction));
                      }
                  }

                  var insertedRelations = new List<Relation>();
                  foreach(var insertRelation in insertRelations)
                  {
                      insertedRelations.Add(await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.Predicate, insertRelation.LayerID, changesetID, transaction));
                  }

                  var affectedCIIDs = createdCIIDs
                  .Concat(removedAttributes.Select(r => r.CIID))
                  .Concat(insertedAttributes.Select(i => i.CIID))
                  .Concat(insertedRelations.SelectMany(i => new long[] { i.FromCIID, i.ToCIID }))
                  .Distinct();
                  var layerSet = await layerModel.BuildLayerSet(layers, transaction);
                  var affectedCIs = await ciModel.GetCIs(layerSet, true, transaction, affectedCIIDs);

                  await transaction.CommitAsync();

                  return MutateReturn.Build(insertedAttributes, removedAttributes, insertedRelations, affectedCIs);
              });
        }
    }
}
