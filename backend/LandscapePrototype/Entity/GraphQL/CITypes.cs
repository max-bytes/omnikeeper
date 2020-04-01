using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using LandscapePrototype.Model.Cached;

namespace LandscapePrototype.Entity.GraphQL
{
    public class CITypeType : ObjectGraphType<Entity.CIType>
    {
        public CITypeType()
        {
            Field("id", x => x.ID);
        }
    }

    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        public MergedCIType(RelationModel relationModel, TemplateModel templateModel)
        {
            Field(x => x.Identity);
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
                
                var CIIdentity = context.Source.Identity;
                var relations = await relationModel.GetMergedRelations(CIIdentity, false, layerset, RelationModel.IncludeRelationDirections.Both, userContext.Transaction, userContext.TimeThreshold);

                var relatedCIs = new List<RelatedCI>();

                var relatedCIIDs = relations.Select(r =>
                {
                    var isForwardRelation = r.FromCIID == CIIdentity;
                    var relatedCIID = (isForwardRelation) ? r.ToCIID : r.FromCIID;
                    return relatedCIID;
                });

                // TODO: consider packing the actual CIs into its own resolver so they can be queried when necessary
                //var CIs = (await ciModel.GetFullCIs(layerset, true, userContext.Transaction, userContext.TimeThreshold, relatedCIIDs)).ToDictionary(ci => ci.Identity);
                foreach(var r in relations)
                {
                    var isForwardRelation = r.FromCIID == CIIdentity;
                    var relatedCIID = (isForwardRelation) ? r.ToCIID : r.FromCIID;
                    relatedCIs.Add(RelatedCI.Build(r, relatedCIID, isForwardRelation));
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
            Field("value", x => x.Value.ToGeneric(), type: typeof(AttributeValueGenericType));
        }
    }

    public class AttributeStateType : EnumerationGraphType<AttributeState>
    {
    }


    public class AttributeValueTypeType : EnumerationGraphType<AttributeValueType>
    {
    }

    public class AttributeValueGenericType : ObjectGraphType<AttributeValueGeneric>
    {
        public AttributeValueGenericType()
        {
            Field(x => x.Type, type: typeof(AttributeValueTypeType));
            Field(x => x.Value);
            Field(x => x.Values);
            Field(x => x.IsArray);
        }
    }
}