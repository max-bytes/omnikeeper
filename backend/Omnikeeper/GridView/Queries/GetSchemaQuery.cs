using FluentValidation;
using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetSchemaQuery
    {
        public class Query : IRequest<(GetSchemaResponse, Exception?)>
        {
            public string Context { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Context).NotEmpty();
            }
        }

        public class GetSchemaQueryHandler : IRequestHandler<Query, (GetSchemaResponse, Exception?)>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public GetSchemaQueryHandler(IGridViewConfigModel gridViewConfigModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewConfigModel = gridViewConfigModel;
                this.modelContextBuilder = modelContextBuilder;
            }
            public async Task<(GetSchemaResponse, Exception?)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();

                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return (new GetSchemaResponse(), ValidationHelper.CreateException(validation));
                }

                var trans = modelContextBuilder.BuildImmediate();

                var config = await gridViewConfigModel.GetConfiguration(request.Context, trans);

                var result = new GetSchemaResponse 
                {
                    ShowCIIDColumn = config.ShowCIIDColumn,
                    Columns = new List<Column>()
                };

                config.Columns.ForEach(el => result.Columns.Add(new Column
                {
                    Name = el.SourceAttributeName,
                    Description = el.ColumnDescription
                }));

                return (result, null);
            }
        }
    }
}
