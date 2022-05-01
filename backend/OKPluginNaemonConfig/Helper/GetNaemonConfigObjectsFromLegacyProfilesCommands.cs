using OKPluginNaemonConfig.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class ConfigObjects
    {
        public static void GetNaemonConfigObjectsFromLegacyProfilesCommands(List<ConfigObj> configObjs, IDictionary<string, Command> commands)
        {
            foreach (var command in commands)
            {
                configObjs.Add(new ConfigObj
                {
                    Type = "command",
                    Attributes = new Dictionary<string, string>
                    {
                        ["command_line"] = command.Value.Exec,
                        ["command_name"] = command.Value.Name,
                    },
                });
            }
        }
    }
}
