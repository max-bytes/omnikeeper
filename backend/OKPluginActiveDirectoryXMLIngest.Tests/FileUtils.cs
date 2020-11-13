using Microsoft.DotNet.InternalAbstractions;
using System;
using System.IO;
using System.Linq;

namespace OKPluginActiveDirectoryXMLIngest.Tests
{
    class FileUtils
    {
        public static string GetFilepath(string filename, string subfolder)
        {
            string startupPath = ApplicationEnvironment.ApplicationBasePath;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            string projectPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, subfolder, filename);
        }

        public static string LoadFile(string filename, string subfolder)
        {
            var path = GetFilepath(filename, subfolder);
            string strText = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return strText;
        }

    }
}
