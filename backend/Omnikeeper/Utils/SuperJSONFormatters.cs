using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Omnikeeper.Startup;
using SpanJson.AspNetCore.Formatter;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Utils
{
    internal class UseSpanJsonAttribute : Attribute, IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => next();
    }

    internal class MySuperInputFormatter : TextInputFormatter
    {
        public MySuperInputFormatter()
        {
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add("application/json");
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var mvcOpt = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
            var formatters = mvcOpt.InputFormatters;
            TextInputFormatter? formatter;

            Endpoint? endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<UseSpanJsonAttribute>() != null)
            {
                formatter = formatters.OfType<SpanJsonInputFormatter<SpanJsonDefaultResolver<byte>>>().FirstOrDefault();
            }
            else
            {
                formatter = formatters.OfType<SystemTextJsonInputFormatter>().FirstOrDefault();
            }
            if (formatter != null)
                return await formatter.ReadRequestBodyAsync(context, encoding);
            else throw new Exception("No suitable input formatter found");
        }
    }

    internal class MySuperOutputFormatter : TextOutputFormatter
    {
        public MySuperOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            
            var mvcOpt = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
            var formatters = mvcOpt.OutputFormatters;
            TextOutputFormatter? formatter;

            Endpoint? endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<UseSpanJsonAttribute>() != null)
            {
                formatter = formatters.OfType<SpanJsonOutputFormatter<SpanJsonDefaultResolver<byte>>>().FirstOrDefault();
            }
            // TODO, HACK: big hack to integrate OData formatters into rest of stack
            else if (endpoint is RouteEndpoint routeEndpoint && routeEndpoint.RoutePattern.PathSegments.Count >= 2 && routeEndpoint.RoutePattern.PathSegments[1].Parts[0] is RoutePatternLiteralPart p && p.Content == "odata")
            {
                // TODO: for some reason, CanWriteResult almost always returns false, even on proper OData response objects
                formatter = formatters.OfType<ODataOutputFormatter>().FirstOrDefault(f => f.CanWriteResult(context));
                if (formatter == null)
                    formatter = formatters.OfType<ODataOutputFormatter>().FirstOrDefault();
            }
            else
            {
                formatter = formatters.OfType<SystemTextJsonOutputFormatter>().FirstOrDefault();
            }
            if (formatter != null)
                await formatter.WriteResponseBodyAsync(context, selectedEncoding);
            else throw new Exception("No suitable output formatter found");
        }
    }
}
