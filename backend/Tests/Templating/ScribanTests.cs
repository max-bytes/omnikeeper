using NUnit.Framework;
using Scriban;
using Scriban.Runtime;
using System;

namespace Tests.Templating
{
    class ScribanTests
    {
        [Test]
        public void TestNestedStuff()
        {
            //var so = new ScriptObjectCI(ci, new ScriptObjectContext(layerSet, trans, atTime, ciModel, relationModel));
            var context = new TemplateContext
            {
                StrictVariables = true
            };
            context.PushGlobal(new ScriptObject() { { "a", new object[] { new { name = "value-a" }, new { name = "value-b" } } } });


            var t = @"{{ a | array.map ""name"" }}";
            var template = Scriban.Template.Parse(t);
            var r = template.Render(context);
            Console.WriteLine(r);
        }

        [Test]
        public void TestReturnNull()
        {
            var context = new TemplateContext
            {
                StrictVariables = true
            };

            var t = @"{{ null }}";
            var template = Scriban.Template.Parse(t);
            var r = template.Evaluate(context);
            Assert.IsNull(r);
        }
    }
}
