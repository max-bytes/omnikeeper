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
        public LandscapeMutation(CIModel ciModel, LayerModel layerModel, TemplateModel templateModel, RelationModel relationModel, ChangesetModel changesetModel, NpgsqlConnection conn)
        {
            FieldAsync<MutateReturnType>("mutate",
              arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                new QueryArgument<ListGraphType<CreateLayerInputType>> { Name = "CreateLayers" },
                new QueryArgument<ListGraphType<InsertCIAttributeInputType>> { Name = "InsertAttributes" },
                new QueryArgument<ListGraphType<RemoveCIAttributeInputType>> { Name = "RemoveAttributes" },
                new QueryArgument<ListGraphType<InsertRelationInputType>> { Name = "InsertRelations" },
                new QueryArgument<ListGraphType<RemoveRelationInputType>> { Name = "RemoveRelations" }
              ),
              resolve: async context =>
              {
                  var layers = context.GetArgument<string[]>("layers");
                  var createLayers = context.GetArgument("CreateLayers", new List<CreateLayerInput>());
                  var insertAttributes = context.GetArgument("InsertAttributes", new List<InsertCIAttributeInput>());
                  var removeAttributes = context.GetArgument("RemoveAttributes", new List<RemoveCIAttributeInput>());
                  var insertRelations = context.GetArgument("InsertRelations", new List<InsertRelationInput>());
                  var removeRelations = context.GetArgument("RemoveRelations", new List<RemoveRelationInput>());

                  var userContext = context.UserContext as LandscapeUserContext;

                  using var transaction = await conn.BeginTransactionAsync();
                  userContext.Transaction = transaction;

                  var changeset = await changesetModel.CreateChangeset(userContext.User.ID, transaction);

                  userContext.LayerSet = await layerModel.BuildLayerSet(layers, transaction);
                  userContext.TimeThreshold = changeset.Timestamp;

                  foreach (var layer in createLayers)
                  {
                      await layerModel.CreateLayer(layer.Name, transaction);
                  }

                  var groupedInsertAttributes = insertAttributes.GroupBy(a => a.CI);
                  var insertedAttributes = new List<CIAttribute>();
                  foreach (var attributeGroup in groupedInsertAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      //var ciid = await ciModel.GetCIIDFromIdentity(ciIdentity, transaction);
                      foreach (var attribute in attributeGroup)
                      {
                          var nonGenericAttributeValue = AttributeValueBuilder.Build(attribute.Value);

                          insertedAttributes.Add(await ciModel.InsertAttribute(attribute.Name, nonGenericAttributeValue, attribute.LayerID, ciIdentity, changeset.ID, transaction));
                      }
                  }

                  var groupedRemoveAttributes = removeAttributes.GroupBy(a => a.CI);
                  var removedAttributes = new List<CIAttribute>();
                  foreach (var attributeGroup in groupedRemoveAttributes)
                  {
                      // look for ciid
                      var ciIdentity = attributeGroup.Key;
                      foreach (var attribute in attributeGroup) {
                          removedAttributes.Add(await ciModel.RemoveAttribute(attribute.Name, attribute.LayerID, ciIdentity, changeset.ID, transaction));
                      }
                  }

                  var insertedRelations = new List<Relation>();
                  foreach(var insertRelation in insertRelations)
                  {
                      insertedRelations.Add(await relationModel.InsertRelation(insertRelation.FromCIID, insertRelation.ToCIID, insertRelation.PredicateID, insertRelation.LayerID, changeset.ID, transaction));
                  }


                  var removedRelations = new List<Relation>();
                  foreach (var removeRelation in removeRelations)
                  {
                      removedRelations.Add(await relationModel.RemoveRelation(removeRelation.FromCIID, removeRelation.ToCIID, removeRelation.PredicateID, removeRelation.LayerID, changeset.ID, transaction));
                  }

                  var affectedCIIDs = removedAttributes.Select(r => r.CIID)
                  .Concat(insertedAttributes.Select(i => i.CIID))
                  .Concat(insertedRelations.SelectMany(i => new string[] { i.FromCIID, i.ToCIID }))
                  .Concat(removedRelations.SelectMany(i => new string[] { i.FromCIID, i.ToCIID }))
                  .Distinct();
                  var affectedCIs = await ciModel.GetMergedCIs(userContext.LayerSet, true, transaction, changeset.Timestamp, affectedCIIDs);

                  var modifiedCIIDsAndLayers = removeAttributes.Select(r => (r.CI, r.LayerID))
                  .Concat(insertAttributes.Select(i => (i.CI, i.LayerID)))
                  .Concat(insertRelations.SelectMany(i => new (string, long)[] { (i.FromCIID, i.LayerID), (i.ToCIID, i.LayerID) }))
                  .Concat(removedRelations.SelectMany(i => new (string, long)[] { (i.FromCIID, i.LayerID), (i.ToCIID, i.LayerID) }))
                  .Distinct();

                  // update template errors
                  // TODO: performance improvements
                  foreach (var affectedCIIDAndLayer in modifiedCIIDsAndLayers)
                    await templateModel.UpdateErrorsOfCI(affectedCIIDAndLayer.Item1, affectedCIIDAndLayer.Item2, ciModel, changeset.ID, transaction);


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
                      createdCIIDs.Add(await ciModel.CreateCIWithType(ci.Identity, ci.TypeID, transaction)); // TODO: add changeset
                  }
                  await transaction.CommitAsync();

                  return CreateCIsReturn.Build(createdCIIDs);
              });
        }
    }
}
