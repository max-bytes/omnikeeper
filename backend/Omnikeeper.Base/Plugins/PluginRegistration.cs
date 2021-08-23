using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Reflection;

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

        IEnumerable<RecursiveTrait> DefinedTraits { get; }
    }

    public abstract class PluginRegistrationBase : IPluginRegistration
    {
        protected PluginRegistrationBase()
        {
            var assembly = GetType().Assembly;
            (Name, Version) = GetPluginNameAndVersionFromAssembly(assembly);
            InformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown version";
        }

        private static (string name, Version version) GetPluginNameAndVersionFromAssembly(Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            if (assemblyName == null)
                throw new Exception("Assembly without name encountered");
            return (assemblyName.Name ?? "Unknown Plugin", assemblyName.Version ?? new Version(0, 0, 0));
        }

        public static TraitOriginV1 GetTraitOrigin(Assembly assembly)
        {
            var (name, _) = GetPluginNameAndVersionFromAssembly(assembly);
            return new TraitOriginV1(TraitOriginType.Plugin, $"CLB-{name}");
        }

        public string Name { get; }
        public Version Version { get; }
        public string InformationalVersion { get; }

        public virtual IPluginDBMigrator? DBMigration { get; } = null;
        public virtual string? ManagementEndpoint { get; } = null;

        public abstract void RegisterServices(IServiceCollection sc);

        public virtual IEnumerable<RecursiveTrait> DefinedTraits { get; } = new RecursiveTrait[0];
    }
}
