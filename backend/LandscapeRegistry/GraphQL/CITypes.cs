using DotLiquid.Util;
using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using Microsoft.AspNetCore.Server.IIS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.GraphQL
{
    public class CITypeType : ObjectGraphType<CIType>
    {
        public CITypeType()
        {
            Field("id", x => x.ID);
            Field(x => x.State, type: typeof(AnchorStateType));
        }
    }

    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        public MergedCIType(RelationModel relationModel, TemplateModel templateModel, TraitModel traitModel, ICIModel ciModel)
        {
            Field("id", x => x.ID);
            Field("name", x => x.Name, nullable: true);
            Field("layerhash", x => x.Layers.LayerHash);
            Field(x => x.AtTime);
            Field(x => x.Type, type: typeof(CITypeType));
            Field(x => x.MergedAttributes, type: typeof(ListGraphType<MergedCIAttributeType>));
            FieldAsync<ListGraphType<RelatedCIType>>("related",
            arguments: new QueryArguments(new List<QueryArgument>
            {
                new QueryArgument<StringGraphType> { Name = "where" },
            }),
            resolve: async (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;
                var layerset = userContext.LayerSet;
                if (layerset == null)
                    throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

                var CIIdentity = context.Source.ID;
                var relations = await relationModel.GetMergedRelations(CIIdentity, false, layerset, IncludeRelationDirections.Both, userContext.Transaction, userContext.TimeThreshold);

                var relatedCIs = new List<RelatedCI>();

                var relationTuples = relations.Select(r =>
                {
                    var isForwardRelation = r.FromCIID == CIIdentity;
                    var relatedCIID = (isForwardRelation) ? r.ToCIID : r.FromCIID;
                    return (relation: r, relatedCIID, isForwardRelation);
                });

                // TODO: consider packing the actual CIs into its own resolver so they can be queried when necessary... but that would open up the 1+N problem again :(
                var relatedCINames = await ciModel.GetCINames(relationTuples.Select(t => t.relatedCIID).Distinct(), layerset, userContext.Transaction, userContext.TimeThreshold);
                foreach ((var relation, var relatedCIID, var isForwardRelation) in relationTuples)
                {
                    relatedCINames.TryGetValue(relatedCIID, out var ciName);
                    relatedCIs.Add(RelatedCI.Build(relation, relatedCIID, ciName, isForwardRelation)); // TODO: rewrite to use CompactCI
                }

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

            FieldAsync<TemplateErrorsCIType>("templateErrors",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;
                return await templateModel.CalculateTemplateErrors(context.Source, userContext.Transaction);
            });

            FieldAsync<ListGraphType<EffectiveTraitType>>("effectiveTraits",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;

                var et = await traitModel.CalculateEffectiveTraitSetForCI(context.Source, userContext.Transaction);
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
            Field(x => x.AtTime);
            Field(x => x.Type, type: typeof(CITypeType));
        }
    }

    public class MergedCIAttributeType : ObjectGraphType<MergedCIAttribute>
    {
        public MergedCIAttributeType(CachedLayerModel layerModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Attribute, type: typeof(CIAttributeType));

            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;
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
            Field("value", x => x.Value.ToGeneric(), type: typeof(AttributeValueDTOType));
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
}