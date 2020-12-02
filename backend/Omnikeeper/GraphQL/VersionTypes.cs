using GraphQL.Types;
using Omnikeeper.Utils;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL
{
    public class VersionDTO
    {
        public string CoreVersion;
        public IEnumerable<ILoadedPlugin> LoadedPlugins;

        public VersionDTO(string coreVersion, IEnumerable<ILoadedPlugin> loadedPlugins)
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
            Field(x => x.LoadedPlugins, type: typeof(ListGraphType<LoadedPluginType>));
        }
    }
    public class LoadedPluginType : ObjectGraphType<ILoadedPlugin>
    {
        public LoadedPluginType()
        {
            Field(x => x.Name);
            Field("version", x => x.Version.ToString());
            Field(x => x.InformationalVersion);
        }
    }

}
