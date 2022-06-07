using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    internal class MetaConfigurationModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            using (var trans = GetService<IModelContextBuilder>().BuildImmediate()) {
                var c1 = await GetService<IMetaConfigurationModel>().GetConfigOrDefault(trans);
                c1.Should().BeEquivalentTo(new MetaConfiguration(new string[] { "__okconfig" }, "__okconfig", new string[] { "__okissues" }, "__okissues"));
            }


            var c2 = new MetaConfiguration(new string[] { "l1", "l2" }, "l3", new string[] { "l4" }, "l5");
            using (var trans = GetService<IModelContextBuilder>().BuildDeferred())
            {
                var c3 = await GetService<IMetaConfigurationModel>().SetConfig(c2, trans);
                c3.Should().BeEquivalentTo(c2);
                trans.Commit();
            }


            using (var trans = GetService<IModelContextBuilder>().BuildImmediate())
            {
                var c4 = await GetService<IMetaConfigurationModel>().GetConfigOrDefault(trans);
                c4.Should().BeEquivalentTo(c2);
            }
        }
    }
}
