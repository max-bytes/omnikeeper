using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;

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
                    new CIAttributeValueConstraintTextRegex("foo[12]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.ECMAScript)
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


            var expectedSerialized = "{\"$type\":\"Omnikeeper.Base.Entity.TraitAttribute, Omnikeeper.Base\",\"AttributeTemplate\":{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeTemplate, Omnikeeper.Base\",\"Name\":\"attributeName\",\"Type\":\"MultilineText\",\"IsArray\":true,\"ValueConstraints\":[{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextRegex, Omnikeeper.Base\",\"RegexStr\":\"foo[12]\",\"RegexOptions\":\"IgnoreCase, ECMAScript\"}],\"IsID\":false},\"Identifier\":\"traitIdentifier\"}";

            var sNewtonsoft = newtonSoftSerializer.SerializeToString(t);
            Assert.AreEqual(expectedSerialized, sNewtonsoft);

            var sSystemTextJson = systemTextJSONSerializer.SerializeToString(t);
            //Assert.AreEqual(expectedSerialized, sSystemTextJson); // we can't expect these to be equal because our new serializer works differently

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
