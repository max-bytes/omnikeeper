using OKPluginNaemonConfig.Entity;
using System;
using System.Collections.Generic;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class ConfigObjects
    {
        public static void GetFromLegacyProfilesGlobalVars(
            List<ConfigObj> configObjs,
            IDictionary<string, Variable> variables
            )
        {
            var variablesAttributes = new Dictionary<string, string>
            {
                ["name"] = "global-variables",
                ["_ALERTS"] = "OFF",
                ["_NRPEPORT"] = "5666"
            };

            foreach (var ciItem in variables)
            {
                // select only variables that have reftype GLOBAL
                if (ciItem.Value.RefType != "GLOBAL" || ciItem.Value.Type != "value")
                {
                    continue;
                }

                string value = ciItem.Value.Value;
                bool isSecret = Convert.ToBoolean(ciItem.Value.IsSecret);

                if (isSecret)
                {
                    value = $"$(python /opt2/nm-agent/bin/getSecret.py {ciItem.Value.Id})";
                }

                var key = $"_{ciItem.Value.Name.ToUpper()}";
                if (!variablesAttributes.ContainsKey(key))
                {
                    variablesAttributes.Add(key, value);
                }
                else if (value != "")
                {
                    variablesAttributes[key] = value;
                }
            }

            configObjs.Add(new ConfigObj
            {
                Type = "host",
                Attributes = variablesAttributes,
            });
        }
    }
}
