using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Text.Json;

namespace Tests.Serialization
{
    public class TraitAttributeTest
    {
        [Test]
        public void TestTraitAttribute()
        {
            var t = new TraitAttribute("traitIdentifier",
                new CIAttributeTemplate("attributeName", Omnikeeper.Entity.AttributeValues.AttributeValueType.MultilineText, true, false, new List<ICIAttributeValueConstraint>()
                {
                    new CIAttributeValueConstraintTextRegex("foo[12]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.ECMAScript),
                    new CIAttributeValueConstraintTextLength(null, 2),
                }));

            var newtonSoftSerializer = new NewtonSoftJSONSerializer<TraitAttribute>(() =>
            {
                var s = new Newtonsoft.Json.JsonSerializerSettings()
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
                };
                s.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                return s;
            });

            var systemTextJSONSerializer = new SystemTextJSONSerializer<TraitAttribute>(() =>
            {
                return new System.Text.Json.JsonSerializerOptions()
                {
                    Converters = {
                        new System.Text.Json.Serialization.JsonStringEnumConverter(),
                    },
                    IncludeFields = true
                };
            });


            var expectedSerializedNewtonsoft = "{\"$type\":\"Omnikeeper.Base.Entity.TraitAttribute, Omnikeeper.Base\",\"AttributeTemplate\":{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeTemplate, Omnikeeper.Base\",\"Name\":\"attributeName\",\"Type\":\"MultilineText\",\"IsArray\":true,\"ValueConstraints\":[{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextRegex, Omnikeeper.Base\",\"RegexStr\":\"foo[12]\",\"RegexOptions\":\"IgnoreCase, ECMAScript\"},{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextLength, Omnikeeper.Base\",\"Minimum\":null,\"Maximum\":2}],\"IsID\":false},\"Identifier\":\"traitIdentifier\"}";
            var expectedSerializedSystemTextJson = "{\"AttributeTemplate\":{\"Name\":\"attributeName\",\"Type\":\"MultilineText\",\"IsArray\":true,\"ValueConstraints\":[{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextRegex, Omnikeeper.Base\",\"RegexStr\":\"foo[12]\",\"RegexOptions\":\"IgnoreCase, ECMAScript\"},{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextLength, Omnikeeper.Base\",\"Minimum\":null,\"Maximum\":2}],\"IsID\":false},\"Identifier\":\"traitIdentifier\"}";

            var sNewtonsoft = newtonSoftSerializer.SerializeToString(t);
            // comparison taken from https://github.com/fluentassertions/fluentassertions/issues/1212
            JsonDocument.Parse(sNewtonsoft).RootElement.Should().BeEquivalentTo(JsonDocument.Parse(expectedSerializedNewtonsoft).RootElement, opt => opt.ComparingByMembers<JsonElement>());

            var sSystemTextJson = systemTextJSONSerializer.SerializeToString(t);
            JsonDocument.Parse(expectedSerializedSystemTextJson).RootElement.Should().BeEquivalentTo(JsonDocument.Parse(sSystemTextJson).RootElement, opt => opt.ComparingByMembers<JsonElement>());
            //Assert.AreEqual(expectedSerializedSystemTextJson, sSystemTextJson);

            var tNewtonsoft = newtonSoftSerializer.Deserialize(sNewtonsoft);
            tNewtonsoft.Should().BeEquivalentTo(t);

            var tSystemTextJson = systemTextJSONSerializer.Deserialize(sSystemTextJson);
            tSystemTextJson.Should().BeEquivalentTo(t);

            // test if systemTextJSON serializer can properly deserialize newtonsoft-serialized object
            var tSystemTextJsonFromSNewtonsoft = systemTextJSONSerializer.Deserialize(sNewtonsoft);
            tSystemTextJsonFromSNewtonsoft.Should().BeEquivalentTo(t);
        }
    }
}
