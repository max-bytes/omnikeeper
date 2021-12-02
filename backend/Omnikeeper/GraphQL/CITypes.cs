using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    // TODO: ci- and layer-based authorization!
    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        public MergedCIType(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel,
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

                var loaded = DataLoaderUtils.SetupAndLoadRelation(RelationSelectionFrom.Build(CIIdentity), dataLoaderContextAccessor, relationModel, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction);
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

                var loaded = DataLoaderUtils.SetupAndLoadRelation(RelationSelectionTo.Build(CIIdentity), dataLoaderContextAccessor, relationModel, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                return loaded.Then(ret =>
                { // TODO: move predicateID filtering into fetch and RelationSelection
                    if (requiredPredicateID != null)
                        return ret.Where(r => r.Relation.PredicateID == requiredPredicateID);
                    else
                        return ret;
                });
            });

            Field<ListGraphType<EffectiveTraitType>>("effectiveTraits",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var ci = context.Source!;

                var loader = DataLoaderUtils.SetupEffectiveTraitLoader(dataLoaderContextAccessor, traitModel, traitsProvider, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                return loader.LoadAsync(ci);
            });
        }
    }

    public class MergedCIAttributeType : ObjectGraphType<MergedCIAttribute>
    {
        public MergedCIAttributeType(IDataLoaderContextAccessor dataLoaderContextAccessor, ILayerModel layerModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Attribute, type: typeof(CIAttributeType));

            Field<ListGraphType<LayerType>>("layerStack",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);

                return DataLoaderUtils.SetupAndLoadAllLayers(dataLoaderContextAccessor, layerModel, timeThreshold, userContext.Transaction)
                    .Then(layers => layers
                        .Where(l => layerstackIDs.Contains(l.ID))
                        .OrderBy(l => layerstackIDs.IndexOf(l.ID))
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
            Field("Value", x => x.Values[0]);
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