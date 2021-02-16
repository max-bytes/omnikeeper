using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Omnikeeper.Base.Plugins
{
    public interface IPluginRegistration
    {
        IPluginDBMigrator? DBMigration { get; }
        void RegisterServices(IServiceCollection sc);

        string? ManagementEndpoint { get; }

        string Name { get; }
        Version Version { get; }
        string InformationalVersion { get; }
    }

    public abstract class PluginRegistrationBase : IPluginRegistration
    {
        protected PluginRegistrationBase()
        {
            var assembly = GetType().Assembly;
            var assemblyName = assembly.GetName();
            if (assemblyName == null)
                throw new Exception("Assembly without name encountered");
            Name = assemblyName.Name ?? "Unknown Plugin";
            Version = assemblyName.Version ?? new Version(0, 0, 0);
            InformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown version";
        }

        public string Name { get; }
        public Version Version { get; }
        public string InformationalVersion { get; }

        public virtual IPluginDBMigrator? DBMigration { get; } = null;
        public virtual string? ManagementEndpoint { get; } = null;

        public abstract void RegisterServices(IServiceCollection sc);
    }
}
