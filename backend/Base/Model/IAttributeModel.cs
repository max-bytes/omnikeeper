﻿using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IAttributeModel : IBaseAttributeModel
    {
        Task<IImmutableDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IImmutableDictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<MergedCIAttribute>> FindMergedAttributesByName(string regex, ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IImmutableDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<MergedCIAttribute> GetMergedAttribute(string name, Guid ciid, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);

    }
}
