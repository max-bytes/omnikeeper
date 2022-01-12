using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
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
                ObjectGraphType wrapperElementGraphType, InputObjectGraphType? idGraphType)
            {
                Name = GenerateTraitEntityRootGraphTypeName(at);

                // select only relevant attributes
                var relevantAttributesForTrait = at.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name)
                    .Concat(at.OptionalAttributes.Select(ra => ra.AttributeTemplate.Name))
                    .ToHashSet();
                var @as = NamedAttributesSelection.Build(relevantAttributesForTrait);


                this.Field("all", new ListGraphType(wrapperElementGraphType), resolve: context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;

                    var ets = dataLoaderService.SetupAndLoadMergedCIs(new AllCIIDsSelection(), @as, false, ciModel, layerset, timeThreshold, trans)
                    .Then(async cis =>
                    {
                        // TODO: use data loader?
                        var ets = await effectiveTraitModel.GetEffectiveTraitsForTrait(at, cis, layerset, trans, timeThreshold);

                        return ets.Select(kv => ((Guid?)kv.Key, kv.Value));
                    });

                    return ets;
                });

                this.Field("byCIID", wrapperElementGraphType,
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" }
                    ),
                    resolve: context =>
                    {
                        var userContext = (context.UserContext as OmnikeeperUserContext)!;
                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;
                        var ciid = context.GetArgument<Guid>("ciid");

                        var t = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(ciid), @as, false, ciModel, layerset, timeThreshold, trans)
                            .Then(async cis =>
                            {
                                var ci = cis.FirstOrDefault();
                                if (ci == null) return (null, null);
                                // TODO: use data loader?
                                var et = await effectiveTraitModel.GetEffectiveTraitForCI(ci, at, layerset, trans, timeThreshold);
                                return ((Guid?)ci.ID, et);
                            });
                        return t;
                    });

                if (idGraphType != null)
                {
                    this.Field("byDataID", wrapperElementGraphType,
                        arguments: new QueryArguments(
                            new QueryArgument(new NonNullGraphType(idGraphType)) { Name = "id" }
                        ),
                        resolve: context =>
                        {
                            var userContext = (context.UserContext as OmnikeeperUserContext)!;
                            var layerset = userContext.GetLayerSet(context.Path);
                            var timeThreshold = userContext.GetTimeThreshold(context.Path);
                            var trans = userContext.Transaction;
                            var idCollection = context.GetArgument(typeof(object), "id") as IDictionary<string, object>;

                            if (idCollection == null)
                                throw new Exception("Invalid input object for trait entity ID detected");

                            var idAttributeValues = InputDictionary2AttributeTuples(idCollection, at)
                                .Where(t => t.isID);

                            // NOTE: we already fetch all relevant attributes for the entities, not JUST the ones necessary for ID checking
                            var t = dataLoaderService.SetupAndLoadMergedCIs(new AllCIIDsSelection(), @as, false, ciModel, layerset, timeThreshold, trans)
                                .Then(async cis =>
                                {
                                    // filter cis
                                    // TODO: improve performance by only fetching CIs with matching attribute values to begin with, not fetch ALL, then filter in code... maybe impossible
                                    var foundCI = cis.Where(ci =>
                                        {
                                            return idAttributeValues.All(nameValue => {
                                                if (ci.MergedAttributes.TryGetValue(nameValue.name, out var a))
                                                    return a.Attribute.Value.Equals(nameValue.value);
                                                return false;
                                            });
                                        })
                                        .OrderBy(ci => ci.ID) // we order by GUID to stay consistent even when multiple CIs would match
                                        .FirstOrDefault();

                                    if (foundCI == null)
                                        return (null, null);

                                    // TODO: use data loader?
                                    var et = await effectiveTraitModel.GetEffectiveTraitForCI(foundCI, at, layerset, trans, timeThreshold);
                                    return ((Guid?)foundCI.ID, et);
                                });

                            return t;
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
                    var (ciid, _) = ((Guid?, EffectiveTrait))context.Source!;
                    if (ciid == null) return null;
                    return ciid.Value;
                });
                this.FieldAsync<MergedCIType>("ci", resolve: async context =>
                {
                    var (ciid, _) = ((Guid?, EffectiveTrait))context.Source!;

                    if (ciid == null || !ciid.HasValue)
                        return null;

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;

                    IAttributeSelection forwardAS = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, trans, timeThreshold);

                    var finalCI = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(ciid.Value), forwardAS, false, ciModel, layerset, timeThreshold, trans)
                        .Then(cis => cis.FirstOrDefault());

                    return finalCI;
                });
                this.Field("entity", elementGraphType, resolve: context =>
                {
                    var (_, et) = ((Guid?, EffectiveTrait))context.Source!;
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
    }

    public class TraitEntitiesQuerySchemaLoader
    {
        private readonly TraitEntitiesType tet;
        private readonly ITraitsProvider traitsProvider;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;

        public TraitEntitiesQuerySchemaLoader(TraitEntitiesType tet, ITraitsProvider traitsProvider,
            IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService)
        {
            this.tet = tet;
            this.traitsProvider = traitsProvider;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.dataLoaderService = dataLoaderService;
        }

        public async Task Init(IModelContext trans, ISchema schema)
        {
            var timeThreshold = TimeThreshold.BuildLatest();
            var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);

            foreach (var at in activeTraits)
            {
                var traitID = at.Key;
                if (traitID == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                var tt = new ElementType(at.Value);
                var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, ciModel);
                var idt = IDInputType.Build(at.Value);
                var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, dataLoaderService, traitsProvider, ttWrapper, idt);

                schema.RegisterTypes(t, ttWrapper, tt);

                var fieldName = GenerateTraitIDFieldName(traitID);

                tet.Field(fieldName, t, resolve: context => t);
            }
        }
    }
}
