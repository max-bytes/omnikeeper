using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class ConfigObjects
    {
        public static void GetFromStaticTemplates(List<ConfigObj> configObjs)
        {
            // NOTE: maybe this need to be configurable
            configObjs.Add(new ConfigObj
            {
                Type = "command",
                Attributes = new Dictionary<string, string>
                {
                    ["command_name"] = "check-nrpe",
                    ["command_line"] = "$USER1$/check_nrpe -2 -t 50 -H $HOSTADDRESS$ -K /opt2/nrpe-ssl/auth.key -C /opt2/nrpe-ssl/auth.crt $ARG1$",
                }
            });
        }
    }
}
