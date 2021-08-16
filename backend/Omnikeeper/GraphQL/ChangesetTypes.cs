﻿using GraphQL.Types;
using GraphQL.Utilities;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;

namespace Omnikeeper.GraphQL
{
    public class UserTypeType : EnumerationGraphType<UserType>
    {
    }

    public class UserInDatabaseType : ObjectGraphType<UserInDatabase>
    {
        public UserInDatabaseType()
        {
            Field("id", x => x.ID);
            Field(x => x.Username);
            Field(x => x.DisplayName);
            Field(x => x.Timestamp);
            Field("type", x => x.UserType, type: typeof(UserTypeType));
        }
    }

    public class ChangesetType : ObjectGraphType<Changeset>
    {
        public ChangesetType()
        {
            Field("id", x => x.ID);
            Field(x => x.Timestamp);
            Field(x => x.User, type: typeof(UserInDatabaseType));
            Field(x => x.LayerID);
            FieldAsync<LayerType>("layer",
            resolve: async (context) =>
            {
                var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerID = context.Source.LayerID;
                return await layerModel.GetLayer(layerID, userContext.Transaction);
            });
        }
    }

}
