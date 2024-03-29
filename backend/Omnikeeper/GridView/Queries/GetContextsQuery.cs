﻿using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Response;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetContextsQuery
    {
        public class Query : IRequest<(GetContextsResponse?, Exception?)>
        {

        }

        public class GetContextsQueryHandler : IRequestHandler<Query, (GetContextsResponse?, Exception?)>
        {
            private readonly GridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly ICurrentUserAccessor currentUserService;
            private readonly IManagementAuthorizationService managementAuthorizationService;

            public GetContextsQueryHandler(GridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder,
                IMetaConfigurationModel metaConfigurationModel, ICurrentUserAccessor currentUserService, IManagementAuthorizationService managementAuthorizationService)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
                this.metaConfigurationModel = metaConfigurationModel;
                this.currentUserService = currentUserService;
                this.managementAuthorizationService = managementAuthorizationService;
            }

            async Task<(GetContextsResponse?, Exception?)> IRequestHandler<Query, (GetContextsResponse?, Exception?)>.Handle(Query request, CancellationToken cancellationToken)
            {
                var trans = modelContextBuilder.BuildImmediate();

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);

                var contexts = await gridViewContextModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

                return (new GetContextsResponse(contexts.Values.ToList()), null);
            }
        }
    }
}
