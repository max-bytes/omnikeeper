using NUnit.Framework;
using System.Collections.Generic;

namespace OKPluginVariableRendering.Tests
{
    public class VariableRenderingTests 
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestAllowedAttribute()
        {
            var res = VariableRendering.IsAttributeAllowed("monitoring.vars.cmdb.interface.ip", new List<string> { "*" }, new List<string> { "monitoring.vars.do_not_include.*" });
            Assert.IsTrue(res);
        }

        [Test]
        public void TestNotAllowedAttribute()
        {
            var res = VariableRendering.IsAttributeAllowed("monitoring.vars.cmdb.interface.ip", new List<string> { "*" }, new List<string> { "monitoring.vars.cmdb.*" });
            Assert.IsFalse(res);
        }

        [Test]
        public void TestAllAttributesInBlacklist()
        {
            var res = VariableRendering.IsAttributeAllowed("monitoring.vars.cmdb.interface.ip", new List<string> { "*" }, new List<string> { "*" });
            Assert.IsFalse(res);
        }

        [Test]
        public void TestAttributeIncludedInSource()
        {
            var res = VariableRendering.IsAttributeIncludedInSource("monitoring.vars.cmdb.interface.ip", "monitoring.vars.*");
            Assert.IsTrue(res);
        }

        [Test]
        public void TestAttributeNotIncludedInSource()
        {
            var res = VariableRendering.IsAttributeIncludedInSource("monitoring.vars.cmdb.interface.ip", "monitoring_vars.*");
            Assert.IsFalse(res);
        }
    }
}