using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Omnikeeper.GraphQL.Types.TraitEntitiesType;

namespace Omnikeeper.GraphQL.Types
{
    public class TraitEntities
    {
    }

    public class TraitEntitiesType : ObjectGraphType<TraitEntities>
    {
        public TraitEntitiesType()
        {
            // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement
            // when there are no traits
            Field<StringGraphType>("placeholder", resolve: ctx => "placeholder");
        }

        private static readonly Regex AllowedNameRegex = new Regex("[^a-zA-Z0-9_]");
        public static string SanitizeTypeName(string unsanitizedTypeName)
        {
            var tmp = unsanitizedTypeName;
            tmp = tmp.Replace(".", "__");

            tmp = AllowedNameRegex.Replace(tmp, "");

            if (tmp.StartsWith("__"))
                tmp = "m" + tmp; // graphql does not support types starting with __, so we prefix it with an "m" (for meta)

            if (Regex.IsMatch(tmp, "[0-9]"))
                tmp = "m" + tmp; // graphql does not support types starting with a digit, so we prefix it with an "m" (for meta)

            return tmp;
        }
        public static string SanitizeFieldName(string unsanitizedFieldName)
        {
            // NOTE: fields and types have same naming rules, so we can re-use
            return SanitizeTypeName(unsanitizedFieldName);
        }
        public static string SanitizeMutationName(string unsanitizedMutationName)
        {
            // NOTE: mutations and types have same naming rules, so we can re-use
            return SanitizeTypeName(unsanitizedMutationName);
        }

        private static string GenerateTraitEntityRootGraphTypeName(ITrait trait) => SanitizeTypeName("TERoot_" + trait.ID);
        private static string GenerateTraitEntityWrapperGraphTypeName(ITrait trait) => SanitizeTypeName("TEWrapper_" + trait.ID);
        private static string GenerateTraitEntityGraphTypeName(ITrait trait) => SanitizeTypeName("TE_" + trait.ID);
        private static string GenerateTraitEntityIDInputGraphTypeName(ITrait trait) => SanitizeTypeName("TE_ID_Input_" + trait.ID);
        private static string GenerateUpsertTraitEntityInputGraphTypeName(ITrait trait) => SanitizeTypeName("TE_Upsert_Input_" + trait.ID);
        public static string GenerateTraitAttributeFieldName(TraitAttribute ta)
        {
            // TODO: what if two unsanitized field names map to the same sanitized field name? TODO: detect this and provide a work-around
            return SanitizeFieldName(ta.Identifier);
        }
        public static string GenerateTraitIDFieldName(string traitID)
        {
            return SanitizeFieldName(traitID);
        }

        public static (string name, IAttributeValue value, bool isID)[] InputDictionary2AttributeTuples(IDictionary<string, object> inputDict, ITrait trait)
        {
            (string name, IAttributeValue value, bool isID)[] attributeValues = inputDict.Select(kv =>
            {
                var inputFieldName = kv.Key;

                // lookup value type based on input attribute name
                var attribute = trait.RequiredAttributes.Concat(trait.OptionalAttributes).FirstOrDefault(ra =>
                {
                    var convertedAttributeFieldName = GenerateTraitAttributeFieldName(ra);
                    return convertedAttributeFieldName == inputFieldName;
                });

                if (attribute == null)
                    throw new Exception($"Invalid input field for trait {trait.ID}: {inputFieldName}");

                var type = attribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                IAttributeValue attributeValue = AttributeValueHelper.BuildFromTypeAndObject(type, kv.Value);
                return (attribute.AttributeTemplate.Name, attributeValue, attribute.AttributeTemplate.IsID.GetValueOrDefault(false));
            }).ToArray();

            return attributeValues;
        }

        public class TraitEntityRootType : ObjectGraphType
        {
            public TraitEntityRootType(ITrait at, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, ITraitsProvider traitsProvider,
                IAttributeModel attributeModel, IRelationModel relationModel,
                ObjectGraphType wrapperElementGraphType, InputObjectGraphType? idGraphType)
            {
                Name = GenerateTraitEntityRootGraphTypeName(at);

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

                            var idAttributeValues = InputDictionary2AttributeTuples(idCollection, at)
                                .Where(t => t.isID)
                                .Select(t => (t.name, t.value))
                                .ToArray();

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

        public class ElementWrapperType : ObjectGraphType
        {
            public ElementWrapperType(ITrait at, ObjectGraphType elementGraphType, ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService, ICIModel ciModel)
            {
                Name = GenerateTraitEntityWrapperGraphTypeName(at);
                    
                this.Field<GuidGraphType>("ciid", resolve: context =>
                {
                    var et = (EffectiveTrait?)context.Source!;
                    return et?.CIID;
                });
                this.FieldAsync<MergedCIType>("ci", resolve: async context =>
                {
                    var et = (EffectiveTrait?)context.Source!;

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
                    var et = (EffectiveTrait?)context.Source!;
                    return et;
                });
            }
        }

        public class ElementType : ObjectGraphType
        {
            public ElementType(ITrait at)
            {
                Name = GenerateTraitEntityGraphTypeName(at);

                foreach (var ta in at.RequiredAttributes.Concat(at.OptionalAttributes))
                {
                    var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));

                    var attributeFieldName = GenerateTraitAttributeFieldName(ta);
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

                // TODO: support for required and optional relations
            }
        }

        public class IDInputType : InputObjectGraphType
        {
            public IDInputType(ITrait at)
            {
                Name = GenerateTraitEntityIDInputGraphTypeName(at);

                foreach (var ta in at.RequiredAttributes)
                {
                    var attributeFieldName = GenerateTraitAttributeFieldName(ta);

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
                Name = GenerateUpsertTraitEntityInputGraphTypeName(at);

                foreach (var ta in at.RequiredAttributes)
                {
                    var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                    AddField(new FieldType()
                    {
                        Name = TraitEntitiesType.GenerateTraitAttributeFieldName(ta),
                        ResolvedType = new NonNullGraphType(graphType)
                    });
                }
                foreach (var ta in at.OptionalAttributes)
                {
                    var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));
                    AddField(new FieldType()
                    {
                        Name = TraitEntitiesType.GenerateTraitAttributeFieldName(ta),
                        ResolvedType = graphType
                    });
                }
                // TODO: add relations
            }
        }
    }

    public class TraitEntitiesQuerySchemaLoader
    {
        private readonly TraitEntitiesType tet;
        private readonly ITraitsProvider traitsProvider;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;

        public TraitEntitiesQuerySchemaLoader(TraitEntitiesType tet, ITraitsProvider traitsProvider, IAttributeModel attributeModel, IRelationModel relationModel,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService)
        {
            this.tet = tet;
            this.traitsProvider = traitsProvider;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
        }

        public class ElementTypesContainer
        {
            public readonly ITrait Trait;
            public readonly ElementType Element;
            public readonly ElementWrapperType ElementWrapper;
            public readonly TraitEntityRootType RootQueryType;
            public readonly IDInputType? IDInputType;
            public readonly UpsertInputType UpsertInputType;

            public ElementTypesContainer(ITrait trait, ElementType element, ElementWrapperType elementWrapper, IDInputType? iDInputType, TraitEntityRootType rootQueryType, UpsertInputType upsertInputType)
            {
                Trait = trait;
                Element = element;
                ElementWrapper = elementWrapper;
                IDInputType = iDInputType;
                RootQueryType = rootQueryType;
                UpsertInputType = upsertInputType;
            }
        }

        public async Task<IEnumerable<ElementTypesContainer>> CreateTypes(IModelContext trans, ISchema schema, ILogger logger)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);

            var ret = new List<ElementTypesContainer>();
            foreach (var at in activeTraits)
            {
                if (at.Key == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                try
                {
                    var tt = new ElementType(at.Value);
                    var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, ciModel);
                    var idt = IDInputType.Build(at.Value);
                    var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, dataLoaderService, traitsProvider, attributeModel, relationModel, ttWrapper, idt);
                    var upsertInputType = new UpsertInputType(at.Value);

                    schema.RegisterTypes(upsertInputType, t);

                    ret.Add(new ElementTypesContainer(at.Value, tt, ttWrapper, idt, t, upsertInputType));
                } catch(Exception e)
                {
                    logger.LogError(e, $"Could not create types for trait entity with trait ID {at.Key}");
                }
            }
            return ret;
        }

        public void Init(IEnumerable<ElementTypesContainer> typesContainers)
        {
            foreach (var typeContainer in typesContainers)
            {
                var traitID = typeContainer.Trait.ID;

                var t = typeContainer.RootQueryType;

                var fieldName = GenerateTraitIDFieldName(traitID);

                tet.Field(fieldName, t, resolve: context => t);
            }
        }
    }
}
