using GraphQL.Types;
using Omnikeeper.Base.Plugins;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL.Types
{
    public class VersionDTO
    {
        public string CoreVersion;
        public IEnumerable<IPluginRegistration> LoadedPlugins;

        public VersionDTO(string coreVersion, IEnumerable<IPluginRegistration> loadedPlugins)
        {
            CoreVersion = coreVersion;
            LoadedPlugins = loadedPlugins;
        }
    }
    public class VersionType : ObjectGraphType<VersionDTO>
    {
        public VersionType()
        {
            Field(x => x.CoreVersion);
            Field(x => x.LoadedPlugins, type: typeof(ListGraphType<PluginRegistrationType>));
        }
    }

    // TODO: move
    public class RunningJob
    {
        public string Name;
        public DateTimeOffset StartedAt;
        public TimeSpan RunningFor;

        public RunningJob(string name, DateTimeOffset startedAt, TimeSpan runningFor)
        {
            Name = name;
            StartedAt = startedAt;
            RunningFor = runningFor;
        }
    }
    public class RunningJobType : ObjectGraphType<RunningJob>
    {
        public RunningJobType()
        {
            Field(x => x.Name);
            Field(x => x.StartedAt);
            Field("runningForMilliseconds", x => x.RunningFor, type: typeof(TimeSpanMillisecondsGraphType));
        }
    }



    public class PluginRegistrationType : ObjectGraphType<IPluginRegistration>
    {
        public PluginRegistrationType()
        {
            Field(x => x.Name);
            Field("version", x => x.Version.ToString());
            Field(x => x.InformationalVersion);
            Field("managementEndpoint", x => x.ManagementEndpoint, nullable: true);
        }
    }

}
