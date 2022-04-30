using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static class FinalConfiguration
    {
        public static string Create(List<ConfigObj> configObjs)
        {
            StringBuilder result = new();

            foreach (var obj in configObjs)
            {
                result.AppendLine($"define {obj.Type} {{");

                foreach (var a in obj.Attributes)
                {
                    if (a.Key.Length <= 31)
                    {
                        //result.AppendLine($"\t{a.Key}{new string(' ', (31-a.Key.Length))}{a.Value}");

                        result.AppendLine($"\t{a.Key.PadRight(30)} {a.Value}");
                    } else
                    {
                        result.AppendLine($"\t{a.Key}\t{a.Value}");
                    }
                    
                }

                result.AppendLine("}");
                result.AppendLine();
            }

            return result.ToString();
        }
    }
}
