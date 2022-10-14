using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Authz;

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
        public MyPermissionsType(ILayerBasedAuthorizationService layerBasedAuthorizationService, IDataLoaderService dataLoaderService)
        {
            Field<ListGraphType<LayerDataType>>("readableLayers")
                .Resolve(context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    return dataLoaderService.SetupAndLoadAllLayers(userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layers => layerBasedAuthorizationService.FilterReadableLayers(userContext.User, layers.Values));
                });
            Field<ListGraphType<LayerDataType>>("writableLayers")
                .Resolve(context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    return dataLoaderService.SetupAndLoadAllLayers(userContext.GetTimeThreshold(context.Path), userContext.Transaction)
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
