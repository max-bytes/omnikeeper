using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class ConfigObjects
    {
        public static void GetFromStaticTemplates(List<ConfigObj> configObjs, StaticTemplateCommand cmd)
        {
            configObjs.Add(new ConfigObj
            {
                Type = "command",
                Attributes = new Dictionary<string, string>
                {
                    ["command_name"] = cmd.Name,
                    ["command_line"] = cmd.Line,
                }
            });
        }
    }
}
