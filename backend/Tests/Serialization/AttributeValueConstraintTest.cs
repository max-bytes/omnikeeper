using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;

namespace Tests.Serialization
{
    internal class AttributeValueConstraintTest
    {
        [Test]
        public void TestTextLength()
        {
            ICIAttributeValueConstraint t = new CIAttributeValueConstraintTextLength(3, null);

            var expectedSerialized = "{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextLength, Omnikeeper.Base\",\"Minimum\":3,\"Maximum\":null}";

            TestSerialization(t, expectedSerialized);
        }

        [Test]
        public void TestTextRegex()
        {
            ICIAttributeValueConstraint t = new CIAttributeValueConstraintTextRegex("foo[12]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.ECMAScript);

            var expectedSerialized = "{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintTextRegex, Omnikeeper.Base\",\"RegexStr\":\"foo[12]\",\"RegexOptions\":\"IgnoreCase, ECMAScript\"}";

            TestSerialization(t, expectedSerialized);
        }

        [Test]
        public void TestArrayLength()
        {
            ICIAttributeValueConstraint t = new CIAttributeValueConstraintArrayLength(null, 12);

            var expectedSerialized = "{\"$type\":\"Omnikeeper.Base.Entity.CIAttributeValueConstraintArrayLength, Omnikeeper.Base\",\"Minimum\":null,\"Maximum\":12}";

            TestSerialization(t, expectedSerialized);
        }

        private void TestSerialization(ICIAttributeValueConstraint t, string expectedSerialized)
        {
            var sNewtonsoft = ICIAttributeValueConstraint.NewtonsoftSerializer.SerializeToString(t);
            Assert.AreEqual(expectedSerialized, sNewtonsoft);

            var sSystemTextJson = ICIAttributeValueConstraint.SystemTextJSONSerializer.SerializeToString(t);
            Assert.AreEqual(expectedSerialized, sSystemTextJson);

            var tNewtonsoft = ICIAttributeValueConstraint.NewtonsoftSerializer.Deserialize(sNewtonsoft);
            tNewtonsoft.Should().BeEquivalentTo(t);

            var tSystemTextJson = ICIAttributeValueConstraint.SystemTextJSONSerializer.Deserialize(sSystemTextJson);
            tSystemTextJson.Should().BeEquivalentTo(t);
        }
    }
}
