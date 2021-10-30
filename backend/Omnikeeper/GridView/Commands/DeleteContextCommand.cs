using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class DeleteContextCommand
    {
        public class Command : IRequest<Exception?>
        {
            public string ID { get; set; }

            public Command(string id)
            {
                this.ID = id;
            }
        }

        public class DeleteContextCommandHandler : IRequestHandler<Command, Exception?>
        {
            private readonly GenericTraitEntityModel<GridViewContext, string> gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly ICurrentUserAccessor currentUserService;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly IChangesetModel changesetModel;
            private readonly IManagementAuthorizationService managementAuthorizationService;

            public DeleteContextCommandHandler(GenericTraitEntityModel<GridViewContext, string> gridViewContextModel, IModelContextBuilder modelContextBuilder, ICurrentUserAccessor currentUserService,
                IMetaConfigurationModel metaConfigurationModel, IChangesetModel changesetModel, IManagementAuthorizationService managementAuthorizationService)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
                this.currentUserService = currentUserService;
                this.metaConfigurationModel = metaConfigurationModel;
                this.changesetModel = changesetModel;
                this.managementAuthorizationService = managementAuthorizationService;
            }

            public async Task<Exception?> Handle(Command request, CancellationToken cancellationToken)
            {
                var timeThreshold = TimeThreshold.BuildLatest();
                using var trans = modelContextBuilder.BuildDeferred();
                var user = await currentUserService.GetCurrentUser(trans);
                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                if (!managementAuthorizationService.CanModifyManagement(user, metaConfiguration, out var message))
                    return new Exception($"User \"{user.Username}\" does not have permission to modify gridview contexts: {message}");

                try
                {
                    var isSuccess = await gridViewContextModel.TryToDelete(request.ID,
                        new Base.Entity.LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                        new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, trans);

                    if (isSuccess)
                    {
                        trans.Commit();
                        return null;
                    }
                    else
                    {
                        return new Exception("An error occured deleting context");
                    }
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }
        }
    }
}
