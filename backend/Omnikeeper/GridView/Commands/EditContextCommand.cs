using FluentValidation;
using MediatR;
using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
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
            public string Name { get; set; }
            public string SpeakingName { get; set; }
            public string Description { get; set; }
            public GridViewConfiguration Configuration { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Name).NotEmpty().NotNull();
                RuleFor(x => x.Configuration.ShowCIIDColumn).NotNull();
                RuleFor(x => x.Configuration.WriteLayer).GreaterThanOrEqualTo(0).WithMessage("WriteLayer should be greater than or equal to 0");
                RuleFor(x => x.Configuration.ReadLayerset).NotEmpty().WithMessage("ReadLayerset should contain at least one item");
                RuleFor(x => x.Configuration.Columns).NotEmpty().WithMessage("Columns should contain at least one item");
                RuleFor(x => x.Configuration.Trait).NotEmpty().NotNull().WithMessage("Trait should not be empty");
            }
        }

        public class EditContextCommandHandler : IRequestHandler<Command, Exception?>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public EditContextCommandHandler(IGridViewConfigModel gridViewConfigModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewConfigModel = gridViewConfigModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            public async Task<Exception?> Handle(Command request, CancellationToken cancellationToken)
            {
                var validator = new CommandValidator();

                var validation = validator.Validate(request);

                if (!validation.IsValid)
                {
                    return ValidationHelper.CreateException(validation);
                }

                using var trans = modelContextBuilder.BuildDeferred();

                try
                {
                    var isSuccess = await gridViewConfigModel.EditContext(request.Name, request.SpeakingName, request.Description, request.Configuration, trans);

                    if (isSuccess)
                    {
                        trans.Commit();
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return ex;
                }

                return new Exception($"An error ocurred trying to edit {request.Name} context!");
            }
        }
    }
}
