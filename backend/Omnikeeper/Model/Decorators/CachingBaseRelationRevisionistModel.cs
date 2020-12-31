using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseRelationRevisionistModel : IBaseRelationRevisionistModel
    {
        private readonly IBaseRelationRevisionistModel model;

        public CachingBaseRelationRevisionistModel(IBaseRelationRevisionistModel model)
        {
            this.model = model;
        }

        public async Task<int> DeleteAllRelations(long layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllRelations(layerID, trans);
            if (numDeleted > 0)
                trans.ClearCache(); // NOTE, HACK, TODO: we'd like to be more specific here, but cache does not support that
            return numDeleted;
        }
    }
}
