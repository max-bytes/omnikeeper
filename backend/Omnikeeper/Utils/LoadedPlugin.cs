using System;

namespace Omnikeeper.Utils
{
    public interface ILoadedPlugin
    {
        string Name { get; }
        Version Version { get; }
        string InformationalVersion { get; }
    }

    public class LoadedPlugin : ILoadedPlugin
    {
        public LoadedPlugin(string name, Version version, string informationalVersion)
        {
            Name = name;
            Version = version;
            InformationalVersion = informationalVersion;
        }

        public string Name { get; }
        public Version Version { get; }
        public string InformationalVersion { get; }
    }
}
