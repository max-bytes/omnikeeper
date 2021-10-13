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
        public static T ExtractMandatoryScalarJSONAttribute<T>(EffectiveTrait et, string traitAttributeName, MyJSONSerializer<T> serializer) where T : class
        {
            var vo = ExtractMandatoryScalarJSONAttribute(et, traitAttributeName);
            var s = serializer.Deserialize(vo);
            if (s == null)
                throw new Exception("Invalid trait configuration");
            return s;
        }

        public static JObject ExtractMandatoryScalarJSONAttribute(EffectiveTrait et, string traitAttributeName)
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
            return vo;
        }

        public static IEnumerable<T> ExtractMandatoryArrayJSONAttribute<T>(EffectiveTrait et, string traitAttributeName, MyJSONSerializer<T> serializer) where T : class
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

        public static IEnumerable<T> ExtractOptionalArrayJSONAttribute<T>(EffectiveTrait et, string traitAttributeName, MyJSONSerializer<T> serializer, IEnumerable<T> @default) where T : class
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


        public static long ExtractMandatoryScalarIntegerAttribute(EffectiveTrait et, string traitAttributeName)
        {
            if (!et.TraitAttributes.TryGetValue(traitAttributeName, out var a))
                throw new Exception("Invalid trait configuration");

            if (a.Attribute.Value is AttributeScalarValueInteger asvi)
                return asvi.Value;

            throw new Exception("Invalid trait configuration");
        }


        public static IEnumerable<MergedRelation> ExtractMandatoryOutgoingRelations(EffectiveTrait et, string traitRelationName)
        {
            if (!et.OutgoingTraitRelations.TryGetValue(traitRelationName, out var relatedCIs))
                throw new Exception("Invalid trait configuration: outgoing trait relation not found");

            return relatedCIs;
        }
        public static IEnumerable<MergedRelation> ExtractMandatoryIncomingRelations(EffectiveTrait et, string traitRelationName)
        {
            if (!et.IncomingTraitRelations.TryGetValue(traitRelationName, out var relatedCIs))
                throw new Exception("Invalid trait configuration: incoming trait relation not found");

            return relatedCIs;
        }
    }
}
