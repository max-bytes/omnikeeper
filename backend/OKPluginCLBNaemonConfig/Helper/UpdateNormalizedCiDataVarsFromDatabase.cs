using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void UpdateVarsFromDatabase(List<ConfigurationItem> ciData)
        {
            // general vars

            //$resultRef[$id]['VARS']['LOCATION'] = (array_key_exists('HLOCATION', $ci['CMDBDATA']) ? replaceNonPrintableChars($ci['CMDBDATA']['HLOCATION']) : '');
            //$resultRef[$id]['VARS']['OS'] = (array_key_exists('HOS', $ci['CMDBDATA']) ? $ci['CMDBDATA']['HOS'] : '');
            //$resultRef[$id]['VARS']['PLATFORM'] = (array_key_exists('HPLATFORM', $ci['CMDBDATA']) ? $ci['CMDBDATA']['HPLATFORM'] : '');

            //$resultRef[$id]['VARS']['ADDRESS'] = (array_key_exists('ADDRESS', $ci) ? $ci['ADDRESS'] : '');
            //$resultRef[$id]['VARS']['PORT'] = (array_key_exists('PORT', $ci) ? $ci['PORT'] : '');

            foreach (var ciItem in ciData)
            {
                if (!ciItem.Vars.ContainsKey("LOCATION"))
                {
                    ciItem.Vars.Add("LOCATION", "");
                }
            }

        }
    }
}
