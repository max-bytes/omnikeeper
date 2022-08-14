using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.Logging.Abstractions;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
{
    public class CLConfigType : ObjectGraphType<CLConfigV1>
    {
        public CLConfigType(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, ILayerDataModel layerDataModel, IDataLoaderService dataLoaderService)
        {
            var existingComputeLayerBrainsDictionary = existingComputeLayerBrains.ToDictionary(clb => clb.Name);

            Field("id", x => x.ID);
            Field("clBrainReference", x => x.CLBrainReference);
            Field("clBrainConfig", x => x.CLBrainConfig.RootElement.ToString());
            Field<ListGraphType<LayerDataType>>("dependentLayers",
            resolve: (context) =>
            {
                var userContext = context.GetUserContext();
                var clBrainReference = context.Source.CLBrainReference;

                if (!existingComputeLayerBrainsDictionary.TryGetValue(clBrainReference, out var clb))
                {
                    return Array.Empty<LayerData>();
                }

                var targetLayerID = ""; // TODO: remove? or get somehow?
                var dependentLayers = clb.GetDependentLayerIDs(targetLayerID, context.Source.CLBrainConfig, NullLogger.Instance);

                return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                    .Then(layersDict => layersDict.Where(kv => dependentLayers.Contains(kv.Key)).Select(kv => kv.Value));
            });
        }
    }
}
