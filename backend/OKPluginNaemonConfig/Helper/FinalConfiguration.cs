using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static class FinalConfiguration
    {

        public static string Create(List<ConfigObj> configObjs)
        {
            var result = "";

            using (var s = new MemoryStream())
            {
                using var sw = new StreamWriter(s);
                foreach (var obj in configObjs)
                {
                    sw.WriteLine($"define {obj.Type} {{");
                    sw.Flush();

                    foreach (var a in obj.Attributes)
                    {
                        sw.WriteLine($"\t{a.Key.ToUpper()}\t{a.Value}");
                        sw.Flush();
                    }

                    sw.WriteLine("}");

                    sw.Flush();

                    sw.WriteLine("");
                    sw.Flush();

                    s.Position = 0;
                }

                using var sr = new StreamReader(s);
                result = sr.ReadToEnd();
            }



            return result ?? "";
        }
    }
}
