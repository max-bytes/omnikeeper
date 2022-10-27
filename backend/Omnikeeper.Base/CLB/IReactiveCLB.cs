using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Omnikeeper.Base.CLB
{
    public interface IReactiveCLB
    {
        string Name { get; }

        IObservable<(bool result, ReactiveRunData runData)> BuildPipeline(IObservable<ReactiveRunData> run, string targetLayerID, JsonDocument clbConfig, ILogger logger);

        ISet<string> GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger);
    }
}
