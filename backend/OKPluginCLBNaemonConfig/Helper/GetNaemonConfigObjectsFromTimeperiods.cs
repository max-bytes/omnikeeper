using OKPluginNaemonConfig.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class ConfigObjects
    {
        public static void GetFromTimeperiods(
            List<ConfigObj> configObjs,
            IDictionary<string, TimePeriod> timeperiods)
        {
            foreach (var ciItem in timeperiods)
            {
                var obj = new ConfigObj
                {
                    Type = "timeperiod",
                    Attributes = new Dictionary<string, string> { 
                        ["timeperiod_name"] = ciItem.Value.Name,
                        ["alias"] = ciItem.Value.Alias,
                    },
                };

                if (ciItem.Value.SpanMon != null && ciItem.Value.SpanMon.Length > 0)
                {
                    obj.Attributes["monday"] = ciItem.Value.SpanMon;
                }

                if (ciItem.Value.SpanTue != null && ciItem.Value.SpanTue.Length > 0)
                {
                    obj.Attributes["tuesday"] = ciItem.Value.SpanTue;
                }

                if (ciItem.Value.SpanWed != null && ciItem.Value.SpanWed.Length > 0)
                {
                    obj.Attributes["wednesday"] = ciItem.Value.SpanWed;
                }

                if (ciItem.Value.SpanThu != null && ciItem.Value.SpanThu.Length > 0)
                {
                    obj.Attributes["thursday"] = ciItem.Value.SpanThu;
                }

                if (ciItem.Value.SpanFri != null && ciItem.Value.SpanFri.Length > 0)
                {
                    obj.Attributes["friday"] = ciItem.Value.SpanFri;
                }

                if (ciItem.Value.SpanSat != null && ciItem.Value.SpanSat.Length > 0)
                {
                    obj.Attributes["saturday"] = ciItem.Value.SpanSat;
                }

                if (ciItem.Value.SpanSun != null && ciItem.Value.SpanSun.Length > 0)
                {
                    obj.Attributes["sunday"] = ciItem.Value.SpanSun;
                }

                configObjs.Add(obj);
            }
        }
    }
}
