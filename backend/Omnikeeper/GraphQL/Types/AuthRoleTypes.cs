using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
{
    public class AuthRoleType : ObjectGraphType<AuthRole>
    {
        public AuthRoleType(IDataLoaderService dataLoaderService, ILayerDataModel layerDataModel, IAuthRolePermissionChecker authRolePermissionChecker)
        {
            Field("id", x => x.ID);
            Field(x => x.Permissions);

            Field<ListGraphType<LayerDataType>>("grantsReadAccessForLayers")
                .Resolve(context =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var permissions = context.Source.Permissions;
                return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                .Then(layersDict =>
                {
                    return layersDict.Values.Where(l => authRolePermissionChecker.DoesAuthRoleGivePermission(context.Source, PermissionUtils.GetLayerReadPermission(l.LayerID)));
                });
            });
            Field<ListGraphType<LayerDataType>>("grantsWriteAccessForLayers")
                .Resolve(context =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var permissions = context.Source.Permissions;
                return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                .Then(layersDict =>
                {
                    return layersDict.Values.Where(l => authRolePermissionChecker.DoesAuthRoleGivePermission(context.Source, PermissionUtils.GetLayerWritePermission(l.LayerID)));
                });
            });
        }
    }
}
