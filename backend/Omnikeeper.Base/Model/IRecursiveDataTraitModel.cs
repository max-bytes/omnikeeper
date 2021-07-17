using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRecursiveDataTraitModel
    {
        public Task<RecursiveTraitSet> GetRecursiveDataTraitSet(IModelContext trans, TimeThreshold timeThreshold);
    }
}
