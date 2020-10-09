using System.Reflection;

namespace Omnikeeper.Service
{
    public static class VersionService
    {
        private static string version = null;
        public static string GetVersion()
        {
            if (version == null)
                version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            return version;
        }
    }
}
