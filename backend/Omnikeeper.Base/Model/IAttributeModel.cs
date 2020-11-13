using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IAttributeModel : IBaseAttributeModel
    {
        Task<IImmutableDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime);
        Task<IImmutableDictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, LayerSet layers, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<MergedCIAttribute>> FindMergedAttributesByName(string regex, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime);
        Task<IImmutableDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime);
        /**
         * NOTE: unlike IAttributeModel.GetAttribute(), GetMergedAttribute() does NOT return removed attributes
         */
        Task<MergedCIAttribute?> GetMergedAttribute(string name, Guid ciid, LayerSet layerset, IModelContext trans, TimeThreshold atTime);
        Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layerset, IModelContext trans, TimeThreshold atTime);

    }
}
