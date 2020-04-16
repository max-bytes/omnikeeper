using GraphQL.Types;
using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

namespace LandscapeRegistry.GraphQL
{
    public class LandscapeMutation : ObjectGraphType
    {
        public LandscapeMutation(CIModel ciModel, AttributeModel attributeModel, LayerModel layerModel, RelationModel relationModel,
            ChangesetModel changesetModel, PredicateModel predicateModel, NpgsqlConnection conn)
        {
            FieldAsync<MutateReturnType>("mutateCIs",
              arguments: new QueryArguments(
                new QueryArgument<ListGraphType<StringGraphType>> { Name = "layers" },
                new QueryArgument<ListGraphType<InsertCIAttributeInputType>> { Name = "InsertAttributes" },
                new QueryArgument<ListGraphType<RemoveCIAttributeInputType>> { Name = "RemoveAttributes" },
                new QueryArgument<ListGraphType<InsertRelationInputType>> { Name = "InsertRelations" },
                new QueryArgument<ListGraphType<RemoveRelationInputType>> { Name = "RemoveRelations" }
              ),
              resolve: async context =>
              {
                  var layers = context.GetArgument<string[]>("layers", null);
                  var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>());
                  var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>());
                  var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>());
                  var removeRelations = context.GetArgument("RemoveRelations", new List<RemoveRelationInput>());

                  var userContext = context.UserContext as LandscapeUserContext;

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var changeset = await changesetModel.CreateChangeset(userContext.User.InDatabase.ID, transaction);

                  userContext.LayerSet = layers != null ? await layerModel.BuildLayerSet(layers, transaction) : null;
                  userContext.TimeThreshold = changeset.Timestamp;

                  var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                  var insertedAttributes = new List<CIAttribute>();
                  foreach (var attributeGroup in groupedInsertAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      foreach (var attribute in attributeGroup)
                      {
                          var nonGenericAttributeValue = AttributeValueBuilder.Build(attribute.Value);

                          insertedAttributes.Add(await attributeModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, attribute.LayerID, ciIdentity, changeset.ID, transaction));
                      }
                  }

                  var groupedRemoveAttributes = removeAttributes.GroupBy(a => a.CI);
                  var removedAttributes = new List<CIAttribute>();
                  foreach (var attributeGroup in groupedRemoveAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      foreach (var attribute in attributeGroup)
                      {
                          removedAttributes.Add(await attributeModel.RemoveAttribute(attribute.Name, attribute.LayerID, ciIdentity, changeset.ID, transaction));
                      }
                  }

                  var insertedRelations = new List<Relation>();
                  foreach (var insertRelation in insertRelations)
                  {
                      insertedRelations.Add(await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.LayerID, changeset.ID, transaction));
                  }

                  var removedRelations = new List<Relation>();
                  foreach (var removeRelation in removeRelations)
                  {
                      removedRelations.Add(await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, removeRelation.LayerID, changeset.ID, transaction));
                  }

                  IEnumerable<MergedCI> affectedCIs = null;
                  if (userContext.LayerSet != null)
                  {
                      var affectedCIIDs = removedAttributes.Select(r => r.CIID)
                      .Concat(insertedAttributes.Select(i => i.CIID))
                      .Concat(insertedRelations.SelectMany(i => new string[] { i.FromCIID, i.ToCIID }))
                      .Concat(removedRelations.SelectMany(i => new string[] { i.FromCIID, i.ToCIID }))
                      .Distinct();
                      affectedCIs = await ciModel.GetMergedCIs(userContext.LayerSet, true, transaction, changeset.Timestamp, affectedCIIDs);
                  }

                  await transaction.CommitAsync();

                  return MutateReturn.Build(insertedAttributes, removedAttributes, insertedRelations, affectedCIs);
              });

            FieldAsync<CreateCIsReturnType>("createCIs",
              arguments: new QueryArguments(
                new QueryArgument<ListGraphType<CreateCIInputType>> { Name = "cis" }
              ),
              resolve: async context =>
              {
                  var createCIs = context.GetArgument("cis", new List<CreateCIInput>());

                  var userContext = context.UserContext as LandscapeUserContext;

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var createdCIIDs = new List<string>();
                  foreach (var ci in createCIs)
                  {
                      createdCIIDs.Add(await ciModel.CreateCIWithType(ci.Identity, ci.TypeID, transaction));
                  }
                  await transaction.CommitAsync();

                  return CreateCIsReturn.Build(createdCIIDs);
              });

            FieldAsync<LayerType>("createLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateLayerInputType>> { Name = "layer" }
              ),
              resolve: async context =>
              {
                  var createLayer = context.GetArgument<CreateLayerInput>("layer");
                  var userContext = context.UserContext as LandscapeUserContext;
                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var createdLayer = await layerModel.CreateLayer(createLayer.Name, createLayer.State, null, transaction);
                  await transaction.CommitAsync();

                  return createdLayer;
              });

            FieldAsync<PredicateType>("upsertPredicate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpsertPredicateInputType>> { Name = "predicate" }
              ),
              resolve: async context =>
              {
                  var predicate = context.GetArgument<UpsertPredicateInput>("predicate");

                  var userContext = context.UserContext as LandscapeUserContext;
                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var newPredicate = await predicateModel.InsertOrUpdate(predicate.ID, predicate.WordingFrom, predicate.WordingTo, predicate.State, transaction);
                  await transaction.CommitAsync();

                  return newPredicate;
              });

            FieldAsync<LayerType>("updateLayer",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<UpdateLayerInputType>> { Name = "layer" }
              ),
              resolve: async context =>
              {
                  var layer = context.GetArgument<UpdateLayerInput>("layer");

                  var userContext = context.UserContext as LandscapeUserContext;
                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var updatedLayer = await layerModel.Update(layer.ID, layer.State, transaction);
                  await transaction.CommitAsync();

                  return updatedLayer;
              });
        }
    }
}
