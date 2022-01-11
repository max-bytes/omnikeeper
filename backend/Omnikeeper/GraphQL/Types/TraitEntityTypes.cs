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

        private static IGraphType TraitAttribute2GraphType(TraitAttribute ta)
        {
            var graphType = ta.AttributeTemplate.Type switch
            {
                AttributeValueType.Text => (IGraphType)new StringGraphType(),
                AttributeValueType.MultilineText => new StringGraphType(),
                AttributeValueType.Integer => new LongGraphType(),
                AttributeValueType.JSON => new StringGraphType(),
                AttributeValueType.YAML => new StringGraphType(),
                AttributeValueType.Image => new StringGraphType(),
                AttributeValueType.Mask => new StringGraphType(),
                _ => throw new NotImplementedException(),
            };
            if (ta.AttributeTemplate.IsArray.GetValueOrDefault(false))
                graphType = new ListGraphType(graphType);

            return graphType;
        }

        public class TraitEntityRootType : ObjectGraphType
        {
            public TraitEntityRootType(ITrait at, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IDataLoaderService dataLoaderService, ITraitsProvider traitsProvider,
                ObjectGraphType wrapperElementGraphType, InputObjectGraphType? idGraphType)
            {
                Name = "TERoot_" + at.ID.Replace(".", "__");

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

                            (string name, IAttributeValue value)[] idAttributeValues = idCollection.Select(kv =>
                            {
                                var inputIDFieldName = kv.Key;

                                // lookup value type based on input attribute name
                                var idAttribute = at.RequiredAttributes.FirstOrDefault(ra =>
                                {
                                    // graphql field names may not contain ".", replace it
                                    // TODO: enforce attribute identifier naming conventions
                                    var convertedAttributeFieldName = ra.Identifier.Replace(".", "__");

                                    return convertedAttributeFieldName == inputIDFieldName;
                                });

                                if (idAttribute == null)
                                    throw new Exception($"Invalid input field for trait entity ID detected: {inputIDFieldName}");

                                var type = idAttribute.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                                IAttributeValue idAttributeValue = AttributeValueBuilder.BuildFromTypeAndObject(type, kv.Value);
                                return (idAttribute.AttributeTemplate.Name, idAttributeValue);
                            }).ToArray();

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
                Name = "TEWrapper_" + at.ID.Replace(".", "__");
                    
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
                Name = "TE_" + at.ID.Replace(".", "__");

                foreach (var ta in at.RequiredAttributes.Concat(at.OptionalAttributes))
                {
                    var graphType = TraitAttribute2GraphType(ta);

                    var valueConverter = ta.AttributeTemplate.Type switch
                    {
                        AttributeValueType.Text => (Func<IAttributeValue, object>)(a => a.ToGenericObject()),
                        AttributeValueType.MultilineText => (Func<IAttributeValue, object>)(a => a.ToGenericObject()),
                        AttributeValueType.Integer => (Func<IAttributeValue, object>)(a => a.ToGenericObject()),
                        AttributeValueType.JSON => (Func<IAttributeValue, object>)(a => (a.IsArray) ? a.ToRawDTOValues() : a.ToRawDTOValues()[0]),
                        AttributeValueType.YAML => (Func<IAttributeValue, object>)(a => (a.IsArray) ? a.ToRawDTOValues() : a.ToRawDTOValues()[0]),
                        AttributeValueType.Image => (Func<IAttributeValue, object>)(a => a.ToGenericObject()),
                        AttributeValueType.Mask => (Func<IAttributeValue, object>)(a => a.ToGenericObject()),
                        _ => throw new NotImplementedException(),
                    };

                    // graphql field names may not contain ".", replace it
                    // TODO: enforce attribute identifier naming conventions
                    var attributeFieldName = ta.Identifier.Replace(".", "__");
                    AddField(new FieldType()
                    {
                        Name = attributeFieldName,
                        ResolvedType = graphType,
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
                                return valueConverter(v.Attribute.Value);
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
                Name = "TE_ID_Input_" + at.ID.Replace(".", "__");

                foreach (var ta in at.RequiredAttributes)
                {
                    // graphql field names may not contain ".", replace it
                    // TODO: enforce attribute identifier naming conventions
                    var attributeFieldName = ta.Identifier.Replace(".", "__");

                    var graphType = TraitAttribute2GraphType(ta);

                    if (ta.AttributeTemplate.IsID.GetValueOrDefault(false))
                    {
                        this.AddField(new FieldType()
                        {
                            Name = attributeFieldName,
                            ResolvedType = graphType
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

    public class TraitEntitiesTypeLoader
    {
        private readonly TraitEntitiesType tet;
        private readonly ITraitsProvider traitsProvider;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IDataLoaderService dataLoaderService;

        public TraitEntitiesTypeLoader(TraitEntitiesType tet, ITraitsProvider traitsProvider,
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
                if (traitID.StartsWith("__"))
                    traitID = "m" + traitID; // graphql does not support fields starting with __, so we prefix it with an "m" (for meta)

                var tt = new ElementType(at.Value);
                var ttWrapper = new ElementWrapperType(at.Value, tt, traitsProvider, dataLoaderService, ciModel);
                var idt = IDInputType.Build(at.Value);
                var t = new TraitEntityRootType(at.Value, effectiveTraitModel, ciModel, dataLoaderService, traitsProvider, ttWrapper, idt);

                schema.RegisterTypes(t, ttWrapper, tt);

                // graphql field names may not contain ".", replace it
                var fieldName = traitID.Replace(".", "__");

                tet.Field(fieldName, t, resolve: context => t);
            }
        }
    }
}
