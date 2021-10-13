﻿using MediatR;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
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
            private readonly GenericTraitEntityModel<GridViewContext, string> gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly ICurrentUserService currentUserService;
            private readonly IManagementAuthorizationService managementAuthorizationService;

            public GetContextsQueryHandler(GenericTraitEntityModel<GridViewContext, string> gridViewContextModel, IModelContextBuilder modelContextBuilder,
                IMetaConfigurationModel metaConfigurationModel, ICurrentUserService currentUserService, IManagementAuthorizationService managementAuthorizationService)
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

                var contexts = await gridViewContextModel.GetAll(metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());

                return (new GetContextsResponse(contexts.Select(t => t.entity).ToList()), null);
            }
        }
    }
}
