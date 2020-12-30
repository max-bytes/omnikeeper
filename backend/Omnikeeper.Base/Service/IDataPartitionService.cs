using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IDataPartitionService
    {
        Task<bool> StartNewPartition();
    }
}
