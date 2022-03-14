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
                    result.AppendLine($"\t{a.Key.ToUpper()}\t{a.Value}");
                }

                result.AppendLine("}");
            }

            return result.ToString();
        }
    }
}
