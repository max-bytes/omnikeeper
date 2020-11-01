using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.GridView.Response;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetSchemaQuery
    {
        public class Query : IRequest<GetSchemaResponse>
        {
            public string Context { get; set; }
        }
        public class GetSchemaQueryHandler : IRequestHandler<Query, GetSchemaResponse>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            public GetSchemaQueryHandler(IGridViewConfigModel gridViewConfigModel)
            {
                this.gridViewConfigModel = gridViewConfigModel;
            }
            public async Task<GetSchemaResponse> Handle(Query request, CancellationToken cancellationToken)
            {

                var config = await gridViewConfigModel.GetConfiguration(request.Context);

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

                return result;
            }
        }
    }
}
