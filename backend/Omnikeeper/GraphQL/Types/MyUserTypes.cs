using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;

namespace Omnikeeper.GraphQL.Types
{
    public class MyUser
    {
        public readonly MyPermissions Permissions = new MyPermissions();
    }
    public class MyPermissions
    {
    }

    public class MyPermissionsType : ObjectGraphType<MyPermissions>
    {
        public MyPermissionsType(ILayerBasedAuthorizationService layerBasedAuthorizationService, ILayerDataModel layerDataModel, IDataLoaderService dataLoaderService)
        {
            Field<ListGraphType<LayerDataType>>("readableLayers",
                resolve: context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layers => layerBasedAuthorizationService.FilterReadableLayers(userContext.User, layers.Values));
                });
            Field<ListGraphType<LayerDataType>>("writableLayers",
                resolve: context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layers => layerBasedAuthorizationService.FilterWritableLayers(userContext.User, layers.Values));
                });
        }
    }
    public class MyUserType : ObjectGraphType<MyUser>
    {
        public MyUserType()
        {
            Field(x => x.Permissions, type: typeof(MyPermissionsType));
        }
    }
}
