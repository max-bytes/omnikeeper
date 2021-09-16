using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Omnikeeper.GraphQL
{

    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        public MergedCIType()
        {
            Field("id", x => x.ID);
            Field("name", x => x.Name, nullable: true);
            Field("layerhash", x => x.Layers.LayerHash);
            Field(x => x.AtTime, type: typeof(TimeThresholdType));
            Field("mergedAttributes", x => x.MergedAttributes.Values, type: typeof(ListGraphType<MergedCIAttributeType>));

            FieldAsync<ListGraphType<MergedRelationType>>("outgoingMergedRelations",
            resolve: async (context) =>
            {
                var ciModel = context.RequestServices!.GetRequiredService<ICIModel>();
                var relationModel = context.RequestServices!.GetRequiredService<IRelationModel>();

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.LayerSet;
                if (layerset == null)
                    throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

                var CIIdentity = context.Source!.ID;

                var relations = await relationModel.GetMergedRelations(new RelationSelectionFrom(CIIdentity), layerset, userContext.Transaction, userContext.TimeThreshold);
                return relations;
            });
            FieldAsync<ListGraphType<MergedRelationType>>("incomingMergedRelations",
            resolve: async (context) =>
            {
                var ciModel = context.RequestServices!.GetRequiredService<ICIModel>();
                var relationModel = context.RequestServices!.GetRequiredService<IRelationModel>();

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.LayerSet;
                if (layerset == null)
                    throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

                var CIIdentity = context.Source!.ID;

                var relations = await relationModel.GetMergedRelations(new RelationSelectionTo(CIIdentity), layerset, userContext.Transaction, userContext.TimeThreshold);
                return relations;
            });

            FieldAsync<ListGraphType<EffectiveTraitType>>("effectiveTraits",
            resolve: async (context) =>
            {
                var traitModel = context.RequestServices!.GetRequiredService<IEffectiveTraitModel>();
                var traitsProvider = context.RequestServices!.GetRequiredService<ITraitsProvider>();

                var userContext = (context.UserContext as OmnikeeperUserContext)!;

                var traits = (await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.TimeThreshold)).Values;

                var et = await traitModel.GetEffectiveTraitsForCI(traits, context.Source!, userContext.Transaction, userContext.TimeThreshold);
                return et;
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
        public MergedCIAttributeType()
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Attribute, type: typeof(CIAttributeType));

            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
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
            Field("value", x => AttributeValueDTO.Build(x.Value), type: typeof(AttributeValueDTOType));
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