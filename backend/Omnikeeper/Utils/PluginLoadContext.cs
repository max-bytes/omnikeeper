using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Omnikeeper.Utils
{
    class PluginLoadContext : AssemblyLoadContext
    {
        private readonly List<AssemblyDependencyResolver> _resolvers = new List<AssemblyDependencyResolver>();

        public PluginLoadContext()
        {
        }

        public void AddResolverFromPath(string pluginPath)
        {
            _resolvers.Add(new AssemblyDependencyResolver(pluginPath));
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            foreach (var r in _resolvers)
            {
                string? assemblyPath = r.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            foreach (var r in _resolvers)
            {
                string? libraryPath = r.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (libraryPath != null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }
            }

            return IntPtr.Zero;
        }
    }
}
