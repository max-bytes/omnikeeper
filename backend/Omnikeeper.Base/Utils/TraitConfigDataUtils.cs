using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<T> DeserializeMandatoryArrayJSONAttribute<T>(EffectiveTrait et, string traitAttributeName, MyJSONSerializer<T> serializer) where T : class
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a)) // empty / no attribute
                throw new Exception("Invalid trait configuration");
            var raa = a.Attribute.Value as AttributeArrayValueJSON;
            if (raa == null)
            {
                throw new Exception("Invalid trait configuration");
            }
            return raa.Values.Select(v =>
            {
                var vo = v.Value as JObject;
                if (vo == null)
                    throw new Exception("Invalid trait configuration");
                var s = serializer.Deserialize(vo);
                if (s == null)
                    throw new Exception("Invalid trait configuration");
                return s;
            });
        }

        public static IEnumerable<T> DeserializeOptionalArrayJSONAttribute<T>(EffectiveTrait et, string traitAttributeName, MyJSONSerializer<T> serializer, IEnumerable<T> @default) where T : class
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a)) // empty / no attribute
                return @default;
            var raa = a.Attribute.Value as AttributeArrayValueJSON;
            if (raa == null)
            {
                throw new Exception("Invalid trait configuration");
            }
            return raa.Values.Select(v =>
            {
                var vo = v.Value as JObject;
                if (vo == null)
                    throw new Exception("Invalid trait configuration");
                var s = serializer.Deserialize(vo);
                if (s == null)
                    throw new Exception("Invalid trait configuration");
                return s;
            });
        }

        public static string ExtractMandatoryScalarTextAttribute(EffectiveTrait et, string traitAttributeName)
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                throw new Exception("Invalid trait configuration");

            if (a.Attribute.Value is AttributeScalarValueText asvt)
                return asvt.Value;

            throw new Exception("Invalid trait configuration");
        }

        public static string ExtractOptionalScalarTextAttribute(EffectiveTrait et, string traitAttributeName, string @default = "")
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                return @default;

            if (a.Attribute.Value is AttributeScalarValueText asvt)
                return asvt.Value;

            throw new Exception("Invalid trait configuration");
        }

        public static IEnumerable<string> ExtractOptionalArrayTextAttribute(EffectiveTrait et, string traitAttributeName, IEnumerable<string> @default)
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                return @default;

            if (a.Attribute.Value is AttributeArrayValueText aavt)
                return aavt.Values.Select(v => v.Value);

            throw new Exception("Invalid trait configuration");
        }
    }
}
