using DotLiquid.Util;
using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Service;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using LandscapeRegistry.Service;
using Microsoft.AspNetCore.Server.IIS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.GraphQL
{

    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        public MergedCIType(IRelationModel relationModel, ITemplateModel templateModel, ITraitModel traitModel, ICIModel ciModel)
        {
            Field("id", x => x.ID);
            Field("name", x => x.Name, nullable: true);
            Field("layerhash", x => x.Layers.LayerHash);
            Field(x => x.AtTime, type: typeof(TimeThresholdType));
            Field("mergedAttributes", x => x.MergedAttributes.Values, type: typeof(ListGraphType<MergedCIAttributeType>));
            FieldAsync<ListGraphType<CompactRelatedCIType>>("related",
            arguments: new QueryArguments(new List<QueryArgument>
            {
                new QueryArgument<StringGraphType> { Name = "where" },
                new QueryArgument<IntGraphType> { Name = "perPredicateLimit" },
            }),
            resolve: async (context) =>
            {
                var userContext = context.UserContext as RegistryUserContext;
                var layerset = userContext.LayerSet;
                if (layerset == null)
                    throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

                var perPredicateLimit = context.GetArgument<int?>("perPredicateLimit");
                if (perPredicateLimit.HasValue && perPredicateLimit.Value <= 0)
                    return new List<CompactRelatedCI>();

                var CIIdentity = context.Source.ID;

                var relatedCIs = await RelationService.GetCompactRelatedCIs(CIIdentity, layerset, ciModel, relationModel, perPredicateLimit, userContext.Transaction, userContext.TimeThreshold);

                var wStr = context.GetArgument<string>("where"); // TODO: develop further
                if (wStr != null)
                    try
                    {
                        relatedCIs = relatedCIs.AsQueryable().Where(wStr).ToList();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                return relatedCIs;
            });

            // TODO: remove?
            FieldAsync<TemplateErrorsCIType>("templateErrors",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as RegistryUserContext;
                return await templateModel.CalculateTemplateErrors(context.Source, userContext.Transaction, userContext.TimeThreshold);
            });

            FieldAsync<ListGraphType<EffectiveTraitType>>("effectiveTraits",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as RegistryUserContext;

                var et = await traitModel.CalculateEffectiveTraitSetForCI(context.Source, userContext.Transaction, userContext.TimeThreshold);
                return et.EffectiveTraits.Values;
            });
        }
    }


    public class CompactCIType : ObjectGraphType<CompactCI>
    {
        public CompactCIType()
        {
            Field("id", x => x.ID);
            Field("name", x => x.Name, nullable: true);
            Field(x => x.AtTime, type: typeof(TimeThresholdType));
            Field("layerhash", x => x.LayerHash);
        }
    }

    public class MergedCIAttributeType : ObjectGraphType<MergedCIAttribute>
    {
        public MergedCIAttributeType(ILayerModel layerModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Attribute, type: typeof(CIAttributeType));

            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as RegistryUserContext;
                var layerstackIDs = context.Source.LayerStackIDs;
                return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
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
            Field(x => x.State, type: typeof(AttributeStateType));
            Field("value", x => x.Value.ToDTO(), type: typeof(AttributeValueDTOType));
        }
    }

    public class AttributeStateType : EnumerationGraphType<AttributeState>
    {
    }


    public class AttributeValueTypeType : EnumerationGraphType<AttributeValueType>
    {
    }

    public class AttributeValueDTOType : ObjectGraphType<AttributeValueDTO>
    {
        public AttributeValueDTOType()
        {
            Field(x => x.Type, type: typeof(AttributeValueTypeType));
            Field("Value", x => x.Values[0]);
            Field(x => x.Values);
            Field(x => x.IsArray);
        }
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