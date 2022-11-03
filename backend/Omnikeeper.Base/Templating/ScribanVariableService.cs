using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Templating
{
    public static class ScribanVariableService
    {
        public class ScriptObjectAttributes : ScriptObject
        {
            public ScriptObjectAttributes(Dictionary<string, object> attributes)
            {
                this.Import("attributes", new Func<Dictionary<string, object>>(() => attributes));
            }
        }
        public static TemplateContext CreateAttributesBasedTemplateContext(Dictionary<string, object> attributes)
        {
            var so = new ScriptObjectAttributes(attributes);
            var context = new TemplateContext
            {
                //context.StrictVariables = true;
                EnableRelaxedMemberAccess = true
            };
            context.PushGlobal(so);
            return context;
        }
    }
}
