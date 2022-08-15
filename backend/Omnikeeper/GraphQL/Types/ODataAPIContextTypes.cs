using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
{
    public class ODataAPIContextType : ObjectGraphType<ODataAPIContext>
    {
        public ODataAPIContextType(ILayerDataModel layerDataModel, IDataLoaderService dataLoaderService)
        {
            Field("id", x => x.ID);
            Field("config", x => ODataAPIContext.ConfigSerializer.SerializeToString(x.CConfig), type: typeof(StringGraphType));
            Field<ListGraphType<LayerDataType>>("readLayers", resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var config = context.Source.CConfig;
                var layerIDs = (config switch
                {
                    ODataAPIContext.ConfigV4 v4 => v4.ReadLayerset,
                    _ => throw new System.Exception("Unknown odata config object")
                }).ToHashSet();
                return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                .Then(layersDict =>
                {
                    return layersDict.Values.Where(l => layerIDs.Contains(l.LayerID));
                });
            });
        }
    }
}
