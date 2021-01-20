using DotLiquid;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Tests.Templating
{

    class CustomDict : Dictionary<string, object>
    {
        private readonly string scalarValue;
        public CustomDict(string scalarValue)
        {
            this.scalarValue = scalarValue;
        }

        public CustomDict(IDictionary<string, object> dictionary, string scalarValue) : base(dictionary)
        {
            this.scalarValue = scalarValue;
        }

        public override string ToString()
        {
            return scalarValue;
        }
    }

    class CustomDrop : Drop
    {
        //private readonly Dictionary<string, object> sub;
        private readonly string scalarValue;
        public CustomDrop(string scalarValue)//Dictionary<string, object> sub, 
        {
            //this.sub = sub;
            this.scalarValue = scalarValue;
        }

        public override string ToString()
        {
            return base.ToString() ?? "";
        }

        public override object BeforeMethod(string method)
        {
            return method;
            //return base.BeforeMethod(method);
        }

        public override object ToLiquid()
        {
            return base.ToLiquid();
        }

        public override object this[object method]
        {
            get
            {
                return base[method];
            }
        }
    }

    class DotLiquidTests
    {
        [Test]
        public void Test()
        {
            {
                var drop = new CustomDrop("a-value");// { { "a", new Dictionary<string, object>() { { "b", "c" }, { "d", "e" } } } };
                var t = "a: {{a}}, b: {{a.b}}, x: {{a._}}";
                var template = Template.Parse(t);
                var r = template.Render(Hash.FromAnonymousObject(new { a = drop }));
                Console.WriteLine(r);
            }
            {
                var variables = new Dictionary<string, object>() { { "a", new Dictionary<string, object>() { { "b", "c" }, { "d", "e" } } } };
                var t = "a: {{a}}, b: {{a.b}}";
                var template = Template.Parse(t);
                var r = template.Render(Hash.FromDictionary(variables));
                Console.WriteLine(r);
            }
            {
                var t = "a: {{a}}, b: {{a.b}}";
                var template = Template.Parse(t);
                var r = template.Render(Hash.FromAnonymousObject(new { a = new { b = 'c', d = 'e' } }));
                Console.WriteLine(r);
            }
            {
                var variables = new CustomDict("a-value") { { "a", new Dictionary<string, object>() { { "b", "c" }, { "d", "e" } } } };
                var t = "a: {{a}}, b: {{a.b}}";
                var template = Template.Parse(t);
                var r = template.Render(Hash.FromDictionary(variables));
                Console.WriteLine(r);
            }
        }
    }
}
