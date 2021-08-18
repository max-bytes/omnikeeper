using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;

namespace Omnikeeper.Base.Utils
{
    public static class TraitConfigDataUtils
    {
        public static T DeserializeMandatoryScalarJSONAttribute<T>(EffectiveTrait et, string traitAttributeName, MyJSONSerializer<T> serializer) where T : class
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                throw new Exception("Invalid trait configuration");
            var raa = a.Attribute.Value as AttributeScalarValueJSON;
            if (raa == null)
            {
                throw new Exception("Invalid trait configuration");
            }
            var vo = raa.Value as JObject;
            if (vo == null)
                throw new Exception("Invalid trait configuration");
            var s = serializer.Deserialize(vo);
            if (s == null)
                throw new Exception("Invalid trait configuration");
            return s;
        }

        public static string ExtractMandatoryScalarTextAttribute(EffectiveTrait et, string traitAttributeName)
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                throw new Exception("Invalid trait configuration");
            return a.Attribute.Value.Value2String();
        }

        public static string ExtractOptionalScalarTextAttribute(EffectiveTrait et, string traitAttributeName, string @default = "")
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                return @default;
            return a.Attribute.Value.Value2String();
        }
    }
}
