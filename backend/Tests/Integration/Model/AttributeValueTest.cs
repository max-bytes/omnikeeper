using NUnit.Framework;
using Omnikeeper.Entity.AttributeValues;

namespace Tests.Integration.Model
{
    class AttributeValueTest
    {
        [Test]
        public void TestBooleanScalarEquality()
        {
            var a1 = new AttributeScalarValueBoolean(true);
            var a2 = new AttributeScalarValueBoolean(true);
            var a3 = new AttributeScalarValueBoolean(false);
            Assert.AreEqual(a1, a2);
            Assert.AreNotEqual(a1, a3);
            Assert.IsTrue(a1.Equals(a2));
            Assert.IsFalse(a1.Equals(a3));
        }
        [Test]
        public void TestBooleanArrayEquality()
        {
            var a1 = AttributeArrayValueBoolean.Build(new bool[] { false, true, false, false, true });
            var a2 = AttributeArrayValueBoolean.Build(new bool[] { false, true, false, false, true });
            var a3 = AttributeArrayValueBoolean.Build(new bool[] { false, true, false, false, false });
            Assert.AreEqual(a1, a2);
            Assert.AreNotEqual(a1, a3);
            Assert.IsTrue(a1.Equals(a2));
            Assert.IsFalse(a1.Equals(a3));
        }

        [Test]
        public void TestBooleanArrayEqualityAsIAttributeValue()
        {
            var a1 = AttributeArrayValueBoolean.Build(new bool[] { false, true, false, false, true });
            var a2 = AttributeArrayValueBoolean.Build(new bool[] { false, true, false, false, true });
            var a3 = AttributeArrayValueBoolean.Build(new bool[] { false, true, false, false, false });
            var i1 = a1 as IAttributeValue;
            var i2 = a2 as IAttributeValue;
            var i3 = a3 as IAttributeValue;
            Assert.AreEqual(i1, i2);
            Assert.AreNotEqual(i1, i3);
            Assert.IsTrue(i1.Equals(i2));
            Assert.IsFalse(i1.Equals(i3));
        }

        //[Test]
        //public void TestTmp()
        //{
        //    var a1 = new Tmp(new bool[] { false, true, false, false, true });
        //    var a2 = new Tmp(new bool[] { false, true, false, false, true });
        //    Assert.AreEqual(a1, a2);
        //}

        //public readonly record struct Tmp(bool[] Values)
        //{
        //    public bool Equals(Tmp other) => Values.SequenceEqual(other.Values);
        //    public override int GetHashCode() => Values.GetHashCode();
        //}
    }
}
