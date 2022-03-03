using MediatR;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
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
            private readonly GenericTraitEntityModel<GridViewContext, string> gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly ICurrentUserAccessor currentUserService;
            private readonly IManagementAuthorizationService managementAuthorizationService;

            public GetContextQueryHandler(GenericTraitEntityModel<GridViewContext, string> gridViewContextModel, IModelContextBuilder modelContextBuilder,
                IMetaConfigurationModel metaConfigurationModel, ICurrentUserAccessor currentUserService, IManagementAuthorizationService managementAuthorizationService)
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

                    var context = await gridViewContextModel.GetSingleByDataID(request.ContextName, metaConfiguration.ConfigLayerset, trans, TimeThreshold.BuildLatest());
                    var result = new GetContextResponse(context.entity);

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
