using FluentValidation;
using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Request;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class AddContextCommand
    {
        public class Command : IRequest<(bool, string)>
        {
            public AddContextRequest Context { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Context.Name).NotEmpty().NotNull();
                RuleFor(x => x.Context.Configuration.ShowCIIDColumn).NotNull();
                RuleFor(x => x.Context.Configuration.WriteLayer).GreaterThan(0).WithMessage("WriteLayer should be greater than 0");
                RuleFor(x => x.Context.Configuration.ReadLayerset).NotEmpty().WithMessage("ReadLayerset should contain at least one item");
                RuleFor(x => x.Context.Configuration.Columns).NotEmpty().WithMessage("Columns should contain at least one item");
                RuleFor(x => x.Context.Configuration.Trait).NotEmpty().NotNull().WithMessage("Trait should not be empty");
            }
        }

        public class AddContextHandler : IRequestHandler<Command, (bool, string)>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            public AddContextHandler(IGridViewConfigModel gridViewConfigModel)
            {
                this.gridViewConfigModel = gridViewConfigModel;
            }

            public async Task<(bool, string)> Handle(Command request, CancellationToken cancellationToken)
            {
                var validator = new CommandValidator();

                var validation = validator.Validate(request);

                if (!validation.IsValid)
                {
                    return (false, ValidationHelper.CreateErrorMessage(validation));
                }

                var isSuccess = await gridViewConfigModel.AddContext(
                    request.Context.Name, 
                    request.Context.SpeakingName, 
                    request.Context.Description,
                    request.Context.Configuration);

                if (isSuccess)
                {
                    return (isSuccess, "");
                }

                return (isSuccess, $"An error ocurred trying to add {request.Context.Name} context!");
            }
        }
    }
}
