using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Text.Json;

namespace Tests.Serialization
{
    public class TraitAttributeTest
    {
        [Test]
        public void TestTraitAttribute()
        {
            var t = new TraitAttribute("traitIdentifier",
                new CIAttributeTemplate("attributeName", Omnikeeper.Entity.AttributeValues.AttributeValueType.MultilineText, true, false, new ICIAttributeValueConstraint[]
                {
                    new CIAttributeValueConstraintTextRegex("foo[12]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.ECMAScript),
                    new CIAttributeValueConstraintTextLength(null, 2),
                }));

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


            var expectedSerializedSystemTextJson = "{\"AttributeTemplate\":{\"Name\":\"attributeName\",\"Type\":\"MultilineText\",\"IsArray\":true,\"ValueConstraints\":[{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextRegex, Omnikeeper.Base\",\"RegexStr\":\"foo[12]\",\"RegexOptions\":\"IgnoreCase, ECMAScript\"},{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextLength, Omnikeeper.Base\",\"Minimum\":null,\"Maximum\":2}],\"IsID\":false},\"Identifier\":\"traitIdentifier\"}";

            var sSystemTextJson = systemTextJSONSerializer.SerializeToString(t);
            JsonDocument.Parse(expectedSerializedSystemTextJson).RootElement.Should().BeEquivalentTo(JsonDocument.Parse(sSystemTextJson).RootElement, opt => opt.ComparingByMembers<JsonElement>());
            //Assert.AreEqual(expectedSerializedSystemTextJson, sSystemTextJson);

            var tSystemTextJson = systemTextJSONSerializer.Deserialize(sSystemTextJson);
            tSystemTextJson.Should().BeEquivalentTo(t);
        }
    }
}
