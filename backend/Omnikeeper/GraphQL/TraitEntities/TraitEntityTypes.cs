using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL.TraitEntities
{

    public class TraitEntities { }

    public class TraitEntitiesType : ObjectGraphType<TraitEntities>
    {
        public TraitEntitiesType()
        {
            // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement
            // when there are no traits
            Field<StringGraphType>("placeholder", resolve: ctx => "placeholder");
        }
    }


    public class TraitEntityRootType : ObjectGraphType
    {
        public TraitEntityRootType(ITrait at, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, ITraitsProvider traitsProvider,
            IAttributeModel attributeModel, IRelationModel relationModel,
            ElementWrapperType wrapperElementGraphType, InputObjectGraphType? idGraphType)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityRootGraphTypeName(at);

            var traitEntityModel = new TraitEntityModel(at, effectiveTraitModel, ciModel, attributeModel, relationModel);

            this.FieldAsync("all", new ListGraphType(wrapperElementGraphType), resolve: async context =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.GetLayerSet(context.Path);
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var trans = userContext.Transaction;

                // TODO: use dataloader
                var ets = await traitEntityModel.GetAllByCIID(layerset, trans, timeThreshold);
                return ets.Select(kv => kv.Value);
            });

            this.FieldAsync("byCIID", wrapperElementGraphType,
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" }
                ),
                resolve: async context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;
                    var ciid = context.GetArgument<Guid>("ciid");

                        // TODO: use dataloader
                        var et = await traitEntityModel.GetSingleByCIID(ciid, layerset, trans, timeThreshold);
                    return et;
                });

            if (idGraphType != null)
            {
                this.FieldAsync("byDataID", wrapperElementGraphType,
                    arguments: new QueryArguments(
                        new QueryArgument(new NonNullGraphType(idGraphType)) { Name = "id" }
                    ),
                    resolve: async context =>
                    {
                        var userContext = (context.UserContext as OmnikeeperUserContext)!;
                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;
                        var idCollection = context.GetArgument(typeof(object), "id") as IDictionary<string, object>;

                        if (idCollection == null)
                            throw new Exception("Invalid input object for trait entity ID detected");

                        var idAttributeValues = TraitEntityHelper.InputDictionary2IDAttributeTuples(idCollection, at);

                            // TODO: use data loader?
                            var foundCIID = await traitEntityModel.GetSingleCIIDByAttributeValueTuples(idAttributeValues, layerset, trans, timeThreshold);

                        if (!foundCIID.HasValue)
                        {
                            return null;
                        }

                        return await traitEntityModel.GetSingleByCIID(foundCIID.Value, layerset, trans, timeThreshold);
                    });
            }
        }
    }

    public class ElementWrapperType : ObjectGraphType<EffectiveTrait>
    {
        public readonly ITrait UnderlyingTrait;

        public ElementWrapperType(ITrait underlyingTrait, ElementType elementGraphType, ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService, ICIModel ciModel, IChangesetModel changesetModel)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityWrapperGraphTypeName(underlyingTrait);

            this.Field<GuidGraphType>("ciid", resolve: context =>
            {
                var et = context.Source;
                return et?.CIID;
            });
            this.FieldAsync<MergedCIType>("ci", resolve: async context =>
            {
                var et = context.Source;

                if (et == null)
                    return null;

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.GetLayerSet(context.Path);
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var trans = userContext.Transaction;

                IAttributeSelection forwardAS = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, trans, timeThreshold);

                var finalCI = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(et.CIID), forwardAS, false, ciModel, layerset, timeThreshold, trans)
                    .Then(cis => cis.FirstOrDefault());

                return finalCI;
            });
            this.Field("entity", elementGraphType, resolve: context =>
            {
                var et = context.Source;
                return et;
            });
            this.Field<ChangesetType>("latestChange", resolve: (context) =>
            {
                var et = context.Source!;

                if (et == null)
                    return null;

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var trans = userContext.Transaction;

                var relevantChangesets =
                    et.TraitAttributes.Select(ta => ta.Value.Attribute.ChangesetID)
                    .Union(et.OutgoingTraitRelations.SelectMany(or => or.Value.Select(o => o.Relation.ChangesetID)))
                    .Union(et.IncomingTraitRelations.SelectMany(or => or.Value.Select(o => o.Relation.ChangesetID)))
                    .ToHashSet();

                return dataLoaderService.SetupAndLoadChangesets(relevantChangesets, changesetModel, trans)
                .Then(changesets =>
                {
                    return changesets.Aggregate((a, b) => a.Timestamp > b.Timestamp ? a : b);
                });
            });


            this.UnderlyingTrait = underlyingTrait;
        }
    }

    public class ElementType : ObjectGraphType<EffectiveTrait>
    {
        public ElementType(ITrait underlyingTrait, ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService, ICIModel ciModel)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityGraphTypeName(underlyingTrait);

            foreach (var ta in underlyingTrait.RequiredAttributes.Concat(underlyingTrait.OptionalAttributes))
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));

                var attributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta);
                AddField(new FieldType()
                {
                    Name = attributeFieldName,
                    ResolvedType = graphType, // TODO: add new NonNullGraphType() wrap for required attributes
                    Resolver = new FuncFieldResolver<object>(ctx =>
                    {
                        var o = ctx.Source as EffectiveTrait;
                        if (o == null)
                        {
                            return null;
                        }

                        var fn = ctx.FieldDefinition.Name;
                        if (o.TraitAttributes.TryGetValue(fn, out var v))
                        {
                            return v.Attribute.Value.ToGraphQLValue();
                        }
                        else return null;
                    })
                });
            }

            foreach (var r in underlyingTrait.RequiredRelations.Concat(underlyingTrait.OptionalRelations))
            {
                var relationFieldName = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(r);
                AddField(new FieldType()
                {
                    Name = relationFieldName,
                    ResolvedType = new ListGraphType(new RelatedCIType(r, traitsProvider, dataLoaderService, ciModel)),
                    Resolver = new FuncFieldResolver<object>(ctx =>
                    {
                        var o = ctx.Source as EffectiveTrait;
                        if (o == null)
                        {
                            return null;
                        }

                        var fn = ctx.FieldDefinition.Name;
                        if (o.IncomingTraitRelations.TryGetValue(fn, out var incomingTraitRelation))
                        {
                            return incomingTraitRelation;
                        }
                        if (o.OutgoingTraitRelations.TryGetValue(fn, out var outgoingTraitRelation))
                        {
                            return outgoingTraitRelation;
                        }
                        else return null;
                    })
                });
            }
        }
    }

    public class IDInputType : InputObjectGraphType
    {
        public IDInputType(ITrait at)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityIDInputGraphTypeName(at);

            foreach (var ta in at.RequiredAttributes)
            {
                var attributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta);

                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));

                if (ta.AttributeTemplate.IsID.GetValueOrDefault(false))
                {
                    this.AddField(new FieldType()
                    {
                        Name = attributeFieldName,
                        ResolvedType = new NonNullGraphType(graphType)
                    });
                }
            }
        }

        public static IDInputType? Build(ITrait at)
        {
            var hasIDFields = at.RequiredAttributes.Any(ra => ra.AttributeTemplate.IsID.GetValueOrDefault(false));
            if (!hasIDFields)
                return null;
            return new IDInputType(at);
        }
    }

    public class UpsertInputType : InputObjectGraphType
    {
        public UpsertInputType(ITrait at)
        {
            Name = TraitEntityTypesNameGenerator.GenerateUpsertTraitEntityInputGraphTypeName(at);

            foreach (var ta in at.RequiredAttributes)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta),
                    ResolvedType = new NonNullGraphType(graphType)
                });
            }
            foreach (var ta in at.OptionalAttributes)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta),
                    ResolvedType = graphType
                });
            }

            foreach (var rr in at.RequiredRelations)
            {
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(rr),
                    ResolvedType = new NonNullGraphType(new ListGraphType(new GuidGraphType()))
                });
            }
            foreach (var rr in at.OptionalRelations)
            {
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(rr),
                    ResolvedType = new ListGraphType(new GuidGraphType())
                });
            }
        }
    }

    public class RelatedCIType : ObjectGraphType<MergedRelation>
    {
        public RelatedCIType(TraitRelation traitRelation, ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService, ICIModel ciModel)
        {
            Name = "RelatedCIType";

            Field<GuidGraphType>("relatedCIID", resolve: context =>
            {
                var relation = context.Source;
                if (traitRelation.RelationTemplate.DirectionForward)
                    return relation?.Relation.ToCIID;
                else
                    return relation?.Relation.FromCIID;
            });
            Field<MergedRelationType>("relation", resolve: context => (MergedRelation)context.Source!);
            this.FieldAsync<MergedCIType>("relatedCI", resolve: async context =>
            {
                var relation = context.Source;

                var otherCIID = (traitRelation.RelationTemplate.DirectionForward) ? relation.Relation.ToCIID : relation.Relation.FromCIID;

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.GetLayerSet(context.Path);
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var trans = userContext.Transaction;

                IAttributeSelection forwardAS = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, trans, timeThreshold);

                var finalCI = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(otherCIID), forwardAS, false, ciModel, layerset, timeThreshold, trans)
                    .Then(cis => cis.FirstOrDefault());

                return finalCI;
            });
        }
    }


    public class MergedCI2TraitEntityWrapper : ObjectGraphType
    {
        public static readonly string StaticName = "MergedCI2TraitEntityWrapper";

        public MergedCI2TraitEntityWrapper(IEnumerable<ElementTypesContainer> typesContainers, IDataLoaderService dataLoaderService, IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider)
        {
            this.Name = StaticName;

            // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement
            // when there are no types
            if (typesContainers.IsEmpty())
            {
                Field<StringGraphType>("placeholder", resolve: ctx => "placeholder");
            }

            foreach (var typeContainer in typesContainers)
            {
                var traitID = typeContainer.Trait.ID;
                var fieldName = TraitEntityTypesNameGenerator.GenerateTraitIDFieldName(traitID);
                this.Field(fieldName, typeContainer.ElementWrapper, resolve: context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var ci = context.Parent?.Source as MergedCI;

                    if (ci == null)
                        throw new Exception("Could not get MergedCI from context... implementation bug?");

                    // NOTE: we assume here that the ci has all relevant attributes loaded for properly checking for effective trait/trait entity
                    // this is ensured through ForwardInspectRequiredAttributes()

                    var ret = dataLoaderService.SetupAndLoadEffectiveTraitLoader(ci, NamedTraitsSelection.Build(traitID), effectiveTraitModel, traitsProvider, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(ets => {
                            var et = ets.FirstOrDefault();
                            return et;
                        });

                    return ret;
                });
            }
        }
    }
}
