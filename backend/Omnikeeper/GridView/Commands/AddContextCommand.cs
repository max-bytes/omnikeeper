using FluentValidation;
using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Request;
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
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Context.Name).NotEmpty().NotNull();
                RuleFor(x => x.Context.Configuration.ShowCIIDColumn).NotNull();
                RuleFor(x => x.Context.Configuration.WriteLayer).GreaterThanOrEqualTo(0).WithMessage("WriteLayer should be greater than or equal to 0");
                RuleFor(x => x.Context.Configuration.ReadLayerset).NotEmpty().WithMessage("ReadLayerset should contain at least one item");
                RuleFor(x => x.Context.Configuration.Columns).NotEmpty().WithMessage("Columns should contain at least one item");
                RuleFor(x => x.Context.Configuration.Trait).NotEmpty().NotNull().WithMessage("Trait should not be empty");
            }
        }

        public class AddContextHandler : IRequestHandler<Command, Exception?>
        {
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public AddContextHandler(IGridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewContextModel = gridViewContextModel;
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
                var trans = modelContextBuilder.BuildDeferred();

                try
                {
                    var isSuccess = await gridViewContextModel.AddContext(
                                            request.Context.Name,
                                            request.Context.SpeakingName,
                                            request.Context.Description,
                                            request.Context.Configuration,
                                            trans);

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

                return new Exception($"An error ocurred trying to add {request.Context.Name} context!");
            }
        }
    }
}
