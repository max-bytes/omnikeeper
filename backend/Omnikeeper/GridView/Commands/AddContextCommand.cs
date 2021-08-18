using FluentValidation;
using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class AddContextCommand
    {
        public class Command : IRequest<Exception?>
        {
            public AddContextRequest Context { get; set; }

            public Command(AddContextRequest Context)
            {
                this.Context = Context;
            }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Context.ID).NotEmpty().NotNull();
                RuleFor(x => x.Context.Configuration.ShowCIIDColumn).NotNull();
                RuleFor(x => x.Context.Configuration.ReadLayerset).NotEmpty().WithMessage("ReadLayerset should contain at least one item");
                RuleFor(x => x.Context.Configuration.Columns).NotEmpty().WithMessage("Columns should contain at least one item");
                RuleFor(x => x.Context.Configuration.Trait).NotEmpty().NotNull().WithMessage("Trait should not be empty");
            }
        }

        public class AddContextHandler : IRequestHandler<Command, Exception?>
        {
            private readonly IGridViewContextWriteService gridViewContextWriteService;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly ICurrentUserService currentUserService;
            private readonly IChangesetModel changesetModel;
            public AddContextHandler(IGridViewContextWriteService gridViewContextWriteService, IModelContextBuilder modelContextBuilder, ICurrentUserService currentUserService, IChangesetModel changesetModel)
            {
                this.gridViewContextWriteService = gridViewContextWriteService;
                this.modelContextBuilder = modelContextBuilder;
                this.currentUserService = currentUserService;
                this.changesetModel = changesetModel;
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

                try
                {
                    await gridViewContextWriteService.InsertOrUpdate(
                        request.Context.ID,
                        request.Context.SpeakingName,
                        request.Context.Description,
                        request.Context.Configuration, 
                        new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual),
                        changesetProxy, user,
                        trans);

                    trans.Commit();
                    return null;
                }
                catch (Exception ex)
                {
                    return new Exception($"An error ocurred trying to add {request.Context.ID} context!", ex);
                }
            }
        }
    }
}
