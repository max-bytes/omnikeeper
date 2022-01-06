using GraphQL.Types;
using Omnikeeper.Base.Plugins;
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
