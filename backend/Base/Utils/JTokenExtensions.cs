using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Utils
{
    public static class JTokenExtensions
    {
        public static bool FullTextSearch(this JToken jToken, string searchString, CompareOptions compareOptions)
        {
            if (jToken is JContainer container)
            {
                return container.Descendants().Where(d => d is JProperty && !(d as JProperty).HasValues).Any(d
                => CultureInfo.InvariantCulture.CompareInfo.IndexOf((d as JProperty).Value.ToString(), searchString, compareOptions) >= 0); // TODO: correct?
            }
            else
            {
                var v = jToken.Value<string>();
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(v, searchString, compareOptions) >= 0;
            }
        }
    }
}
