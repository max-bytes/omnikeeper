using FluentValidation;
using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class EditContextCommand
    {
        public class Command : IRequest<Exception?>
        {
            public string ID { get; set; }
            public string SpeakingName { get; set; }
            public string Description { get; set; }
            public GridViewConfiguration Configuration { get; set; }

            public Command(string id, string SpeakingName, string Description, GridViewConfiguration Configuration)
            {
                this.ID = id;
                this.SpeakingName = SpeakingName;
                this.Description = Description;
                this.Configuration = Configuration;
            }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.ID).NotEmpty().NotNull();
                RuleFor(x => x.Configuration.ShowCIIDColumn).NotNull();
                RuleFor(x => x.Configuration.ReadLayerset).NotEmpty().WithMessage("ReadLayerset should contain at least one item");
                RuleFor(x => x.Configuration.Columns).NotEmpty().WithMessage("Columns should contain at least one item");
                RuleFor(x => x.Configuration.Trait).NotEmpty().NotNull().WithMessage("Trait should not be empty");
            }
        }

        public class EditContextCommandHandler : IRequestHandler<Command, Exception?>
        {
            private readonly GenericTraitEntityModel<GridViewContext, string> gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly ICurrentUserService currentUserService;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly IChangesetModel changesetModel;
            private readonly IManagementAuthorizationService managementAuthorizationService;

            public EditContextCommandHandler(IModelContextBuilder modelContextBuilder, GenericTraitEntityModel<GridViewContext, string> gridViewContextModel, ICurrentUserService currentUserService,
                IMetaConfigurationModel metaConfigurationModel, IChangesetModel changesetModel, IManagementAuthorizationService managementAuthorizationService)
            {
                this.modelContextBuilder = modelContextBuilder;
                this.gridViewContextModel = gridViewContextModel;
                this.currentUserService = currentUserService;
                this.metaConfigurationModel = metaConfigurationModel;
                this.changesetModel = changesetModel;
                this.managementAuthorizationService = managementAuthorizationService;
            }

            public async Task<Exception?> Handle(Command request, CancellationToken cancellationToken)
            {
                var validator = new CommandValidator();

                var validation = validator.Validate(request);

                if (!validation.IsValid)
                {
                    return ValidationHelper.CreateException(validation);
                }

                var timeThreshold = TimeThreshold.BuildLatest();
                using var trans = modelContextBuilder.BuildDeferred();
                var user = await currentUserService.GetCurrentUser(trans);
                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                if (!managementAuthorizationService.CanModifyManagement(user, metaConfiguration, out var message))
                    return new Exception($"User \"{user.Username}\" does not have permission to modify gridview contexts: {message}");

                var update = new GridViewContext(request.ID, request.SpeakingName, request.Description, request.Configuration);
                try
                {
                    await gridViewContextModel.InsertOrUpdate(update,
                        new Base.Entity.LayerSet(metaConfiguration.ConfigLayerset), metaConfiguration.ConfigWriteLayer,
                        new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                        changesetProxy,
                        trans);

                    trans.Commit();
                    return null;
                }
                catch (Exception ex)
                {
                    return new Exception($"An error ocurred trying to edit {request.ID} context!", ex);
                }

            }
        }
    }
}
