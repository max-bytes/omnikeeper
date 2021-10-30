using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IUsageDataAccumulator
    {
        void Add(string username, DateTimeOffset timestamp, IEnumerable<(string elementType, string elementName)> elements);
        void Flush(IModelContext trans);
        Task<int> DeleteOlderThan(DateTimeOffset deleteThreshold, IModelContext trans);
    }
}
