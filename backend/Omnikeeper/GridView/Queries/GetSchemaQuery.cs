using FluentValidation;
using MediatR;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetSchemaQuery
    {
        public class Query : IRequest<(GetSchemaResponse?, Exception?)>
        {
            public string Context { get; set; }
            public Query(string Context)
            {
                this.Context = Context;
            }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Context).NotEmpty();
            }
        }

        public class GetSchemaQueryHandler : IRequestHandler<Query, (GetSchemaResponse?, Exception?)>
        {
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public GetSchemaQueryHandler(IGridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
            }
            public async Task<(GetSchemaResponse?, Exception?)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();

                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return (null, ValidationHelper.CreateException(validation));
                }

                var trans = modelContextBuilder.BuildImmediate();

                var context = await gridViewContextModel.GetFullContextByName(request.Context, trans);
                var config = context.Configuration;

                var result = new GetSchemaResponse(config.ShowCIIDColumn, new List<Column>());

                config.Columns.ForEach(el => result.Columns.Add(new Column
                (
                    el.SourceAttributeName,
                    el.ColumnDescription,
                    el.ValueType ?? AttributeValueType.Text,
                    el.WriteLayer != ""
                )));

                return (result, null);
            }
        }
    }
}
