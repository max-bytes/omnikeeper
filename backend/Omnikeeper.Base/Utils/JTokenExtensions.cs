using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Linq;

namespace Omnikeeper.Base.Utils
{
    public static class JTokenExtensions
    {
        public static bool FullTextSearch(this JToken jToken, string searchString, CompareOptions compareOptions)
        {
            if (jToken is JContainer container)
            {
                return container.Descendants().Where(d => d is JProperty jd && !jd.HasValues).Any(d
                => CultureInfo.InvariantCulture.CompareInfo.IndexOf((d as JProperty)!.Value.ToString(), searchString, compareOptions) >= 0); // TODO: correct?
            }
            else
            {
                var v = jToken.Value<string>();
                return CultureInfo.InvariantCulture.CompareInfo.IndexOf(v, searchString, compareOptions) >= 0;
            }
        }
    }
}
