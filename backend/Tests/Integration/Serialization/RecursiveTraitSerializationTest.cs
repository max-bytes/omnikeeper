using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tests.Integration.Serialization
{
    class RecursiveTraitSerializationTest
    {
        [Test]
        public void TestSerialization()
        {
            var dataSerializer = new ProtoBufDataSerializer();
            var a = new RecursiveTrait("rt1", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
            {
                new TraitAttribute("at1", CIAttributeTemplate.BuildFromParams("name", AttributeValueType.JSON, true, 
                    new CIAttributeValueConstraintTextLength(2, null), 
                    new CIAttributeValueConstraintTextRegex(new Regex("(RedHat|Fedora)", RegexOptions.IgnoreCase | RegexOptions.Singleline)))
                )
            });
            var b = dataSerializer.ToByteArray(a);
            var c = dataSerializer.FromByteArray<RecursiveTrait>(b);
            c.Should().BeEquivalentTo(a);
        }
    }
}
