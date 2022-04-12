using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL.TraitEntities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL.Types
{
    // TODO: ci-based authorization!
    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        public MergedCIType(IDataLoaderService dataLoaderService, IRelationModel relationModel,
            IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider)
        {
            Field("id", x => x.ID);
            Field("name", x => x.CIName, nullable: true);
            Field("layerhash", x => x.Layers.LayerHash);
            Field(x => x.AtTime, type: typeof(TimeThresholdType));
            Field<ListGraphType<MergedCIAttributeType>>("mergedAttributes",
                arguments: new QueryArguments(new QueryArgument<ListGraphType<StringGraphType>> { Name = "attributeNames" }),
                resolve: context =>
                {
                    var mergedAttributes = context.Source!.MergedAttributes.Values;

                    // NOTE: the outer caller/resolver should already have filtered the attributes
                    // but because we cannot be sure of that, we still do this filtering here too, even if its redundant
                    var attributeNames = context.GetArgument<string[]?>("attributeNames", null)?.ToHashSet();
                    if (attributeNames != null)
                        return mergedAttributes.Where(a => attributeNames.Contains(a.Attribute.Name));
                    return mergedAttributes;
                }
            );

            Field<ListGraphType<MergedRelationType>>("outgoingMergedRelations",
            arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "requiredPredicateID" }),
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var CIIdentity = context.Source!.ID;
                var requiredPredicateID = context.GetArgument<string?>("requiredPredicateID", null);

                var loaded = dataLoaderService.SetupAndLoadRelation(RelationSelectionFrom.Build(CIIdentity), relationModel, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                return loaded.Then(ret =>
                { // TODO: move predicateID filtering into fetch and RelationSelection
                    if (requiredPredicateID != null)
                        return ret.Where(r => r.Relation.PredicateID == requiredPredicateID);
                    else
                        return ret;
                });
            });
            Field<ListGraphType<MergedRelationType>>("incomingMergedRelations",
            arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "requiredPredicateID" }),
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var CIIdentity = context.Source!.ID;
                var requiredPredicateID = context.GetArgument<string?>("requiredPredicateID", null);

                var loaded = dataLoaderService.SetupAndLoadRelation(RelationSelectionTo.Build(CIIdentity), relationModel, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                return loaded.Then(ret =>
                { // TODO: move predicateID filtering into fetch and RelationSelection
                    if (requiredPredicateID != null)
                        return ret.Where(r => r.Relation.PredicateID == requiredPredicateID);
                    else
                        return ret;
                });
            });

            Field<ListGraphType<EffectiveTraitType>>("effectiveTraits",
            arguments: new QueryArguments(new QueryArgument<ListGraphType<StringGraphType>> { Name = "traitIDs" }),
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var ci = context.Source!;

                ITraitSelection traitSelection = AllTraitsSelection.Instance;
                var traitIDs = context.GetArgument<string[]?>("traitIDs", null)?.ToHashSet();
                if (traitIDs != null)
                    traitSelection = NamedTraitsSelection.Build(traitIDs);

                var ret = dataLoaderService.SetupAndLoadEffectiveTraits(ci, traitSelection, traitModel, traitsProvider, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                return ret;
            });
        }

        public static async Task<IAttributeSelection> ForwardInspectRequiredAttributes(IResolveFieldContext context, ITraitsProvider traitsProvider, IModelContext trans, TimeThreshold timeThreshold)
        {
            // do a "forward" look into the graphql query to see which attributes we actually need to fetch to properly fulfill the request
            // because we need to at least fetch a single attribute (due to internal reasons), we might as well fetch the name attribute and then don't care if it is requested or not
            IAttributeSelection baseAttributeSelection = NamedAttributesSelection.Build(ICIModel.NameAttribute);
            IAttributeSelection attributeSelectionBecauseOfMergedAttributes = NoAttributesSelection.Instance;
            IAttributeSelection attributeSelectionBecauseOfTraits = NoAttributesSelection.Instance;
            IAttributeSelection attributeSelectionBecauseOfTraitEntities = NoAttributesSelection.Instance;
            if (context.SubFields != null && context.SubFields.TryGetValue("mergedAttributes", out var mergedAttributesField))
            {
                // check whether or not the attributeNames parameter was set, in which case we can reduce the attributes to query for
                var attributeNamesArgument = mergedAttributesField.Field.Arguments?.FirstOrDefault(a => a.Name == "attributeNames");
                if (attributeNamesArgument != null && attributeNamesArgument.Value is GraphQLListValue lv)
                {
                    if (lv.Values == null)
                        throw new Exception("Unknown error");
                    var attributeNames = lv.Values.Select(v =>
                    {
                        if (v is not GraphQLStringValue sv)
                            throw new Exception("argumentNames must be specified as list of strings");
                        return sv.Value.ToString();
                    }).ToHashSet();
                    attributeSelectionBecauseOfMergedAttributes = NamedAttributesSelection.Build(attributeNames);
                }
                else
                {
                    // we need to query all attributes
                    attributeSelectionBecauseOfMergedAttributes = AllAttributeSelection.Instance;
                }
            }
            if (context.SubFields != null && context.SubFields.TryGetValue("effectiveTraits", out var effectiveTraitsField))
            {
                // reduce the required attributes by checking the requested effective traits and respecting their required and optional attributes
                var traitIDsArgument = effectiveTraitsField.Field.Arguments?.FirstOrDefault(a => a.Name.StringValue == "traitIDs");
                if (traitIDsArgument != null && traitIDsArgument.Value is GraphQLListValue lv)
                {
                    if (lv.Values == null)
                        throw new Exception("Unknown error");
                    var requestedTraitIDs = lv.Values.Select(v =>
                    {
                        if (v is not GraphQLStringValue sv)
                            throw new Exception("traitIDs must be specified as list of strings");
                        return sv.Value.ToString();
                    }).ToHashSet();

                    // TODO: more performant, rely on dictionary
                    var allTraits = (await traitsProvider.GetActiveTraits(trans, timeThreshold)).Values;
                    var requestedTraits = allTraits.Where(t => requestedTraitIDs.Contains(t.ID));

                    var relevantAttributesForTraits = requestedTraits.SelectMany(t =>
                    t.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Union(
                    t.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name))
                    ).ToHashSet();

                    attributeSelectionBecauseOfTraits = NamedAttributesSelection.Build(relevantAttributesForTraits);
                }
                else
                {
                    attributeSelectionBecauseOfTraits = AllAttributeSelection.Instance;
                }
            }
            if (context.SubFields != null && context.SubFields.TryGetValue("traitEntity", out var traitEntityField))
            {
                var selectedTraitEntityFields = traitEntityField.Field.SelectionSet?.Selections;

                if (selectedTraitEntityFields != null)
                {
                    var parentGraphType = traitEntityField.FieldType.ResolvedType as IObjectGraphType;
                    if (parentGraphType == null) throw new Exception();

                    // from the field name, resolve the actual trait entity name
                    foreach (var selection in selectedTraitEntityFields)
                    {
                        var field = selection as GraphQLField;
                        if (field == null) throw new Exception();
                        // TODO: can this be improved?
                        var ft = _GetAroundProtectedFunctions.PublicGetFieldDefinition(context.Schema, parentGraphType, field);
                        var rt = ft?.ResolvedType as ElementWrapperType;
                        var underlyingTrait = rt?.UnderlyingTrait;
                        if (underlyingTrait == null) throw new Exception();

                        var @as = NamedAttributesSelection.Build(
                            underlyingTrait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Union(underlyingTrait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet()
                        );
                        attributeSelectionBecauseOfTraitEntities = attributeSelectionBecauseOfTraitEntities.Union(@as);
                    }
                }
            }

            var finalAttributeSelection = baseAttributeSelection
                .Union(attributeSelectionBecauseOfMergedAttributes)
                .Union(attributeSelectionBecauseOfTraits)
                .Union(attributeSelectionBecauseOfTraitEntities);
            return finalAttributeSelection;
        }

        private static readonly GetAroundProtectedFunctions _GetAroundProtectedFunctions = new();

        private class GetAroundProtectedFunctions : ExecutionStrategy
        {

            public FieldType? PublicGetFieldDefinition(ISchema schema, IObjectGraphType parentType, GraphQLField field)
            {
                return base.GetFieldDefinition(schema, parentType, field);
            }

            public override Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class MergedCIAttributeType : ObjectGraphType<MergedCIAttribute>
    {
        public MergedCIAttributeType(IDataLoaderService dataLoaderService, ILayerDataModel layerDataModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Attribute, type: typeof(CIAttributeType));

            Field<ListGraphType<LayerDataType>>("layerStack",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);

                return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, timeThreshold, userContext.Transaction)
                    .Then(layers => layers
                        .Where(kv => layerstackIDs.Contains(kv.Key))
                        .OrderBy(kv => layerstackIDs.IndexOf(kv.Key))
                        .Select(kv => kv.Value)
                    );
            });
        }
    }

    public class CIAttributeType : ObjectGraphType<CIAttribute>
    {
        public CIAttributeType()
        {
            Field("id", x => x.ID);
            Field("ciid", x => x.CIID);
            Field(x => x.ChangesetID);
            Field(x => x.Name);
            Field("value", x => AttributeValueDTO.Build(x.Value), type: typeof(AttributeValueDTOType));
        }
    }


    public class AttributeValueTypeType : EnumerationGraphType<AttributeValueType>
    {
    }

    public class AttributeValueDTOType : ObjectGraphType<AttributeValueDTO>
    {
        public AttributeValueDTOType()
        {
            Field(x => x.Type, type: typeof(AttributeValueTypeType));
            Field("value", x => x.Values[0]);
            Field(x => x.Values);
            Field(x => x.IsArray);
        }
    }

    public class DataOriginGQL : ObjectGraphType<DataOriginV1>
    {
        public DataOriginGQL()
        {
            Field(x => x.Type, type: typeof(DataOriginTypeGQL));
        }
    }

    public class DataOriginTypeGQL : EnumerationGraphType<DataOriginType>
    {
    }

    public class TimeThresholdType : ObjectGraphType<TimeThreshold>
    {
        public TimeThresholdType()
        {
            Field(x => x.Time);
            Field(x => x.IsLatest);
        }
    }
}