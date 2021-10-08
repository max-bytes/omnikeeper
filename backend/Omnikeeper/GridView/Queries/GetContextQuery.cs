using MediatR;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Response;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetContextQuery
    {
        public class Query : IRequest<(GetContextResponse?, Exception?)>
        {
            public string ContextName { get; set; }

            public Query(string ContextName)
            {
                this.ContextName = ContextName;
            }
        }

        public class GetContextQueryHandler : IRequestHandler<Query, (GetContextResponse?, Exception?)>
        {
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly ICurrentUserService currentUserService;
            private readonly IManagementAuthorizationService managementAuthorizationService;

            public GetContextQueryHandler(IGridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder,
                IMetaConfigurationModel metaConfigurationModel, ICurrentUserService currentUserService, IManagementAuthorizationService managementAuthorizationService)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
                this.metaConfigurationModel = metaConfigurationModel;
                this.currentUserService = currentUserService;
                this.managementAuthorizationService = managementAuthorizationService;
            }

            async Task<(GetContextResponse?, Exception?)> IRequestHandler<Query, (GetContextResponse?, Exception?)>.Handle(Query request, CancellationToken cancellationToken)
            {
                try
                {
                    var trans = modelContextBuilder.BuildImmediate();
                    var user = await currentUserService.GetCurrentUser(trans);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                    if (!managementAuthorizationService.CanReadManagement(user, metaConfiguration, out var message))
                        return (null, new Exception($"User \"{user.Username}\" does not have permission to read gridview context: {message}"));

                    var context = await gridViewContextModel.GetFullContext(request.ContextName, metaConfiguration.ConfigLayerset, TimeThreshold.BuildLatest(), trans);
                    var result = new GetContextResponse(context);

                    return (result, null);
                }
                catch (Exception e)
                {
                    return (null, e);
                }
            }
        }
    }
}
