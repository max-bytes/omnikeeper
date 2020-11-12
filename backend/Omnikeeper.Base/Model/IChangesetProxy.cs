﻿using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IChangesetProxy
    {
        Task<Changeset> GetChangeset(IModelContext trans);
        DateTimeOffset Timestamp { get; }
    }

}
