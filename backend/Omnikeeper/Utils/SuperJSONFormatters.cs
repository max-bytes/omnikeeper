using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Omnikeeper.Startup;
using SpanJson.AspNetCore.Formatter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Utils
{
    internal class UseSpanJsonAttribute : Attribute, IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => next();
    }

    internal class MySuperJsonInputFormatter : TextInputFormatter
    {
        public MySuperJsonInputFormatter()
        {
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add("application/json");
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var mvcOpt = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
            var formatters = mvcOpt.InputFormatters;
            TextInputFormatter? formatter = null;

            Endpoint endpoint = context.HttpContext.GetEndpoint();
            if (endpoint.Metadata.GetMetadata<UseSpanJsonAttribute>() != null)
            {
                formatter = formatters.OfType<SpanJsonInputFormatter<SpanJsonDefaultResolver<byte>>>().FirstOrDefault();
            }
            else
            {
                formatter = (NewtonsoftJsonInputFormatter)(formatters
                    .Where(f => typeof(NewtonsoftJsonInputFormatter) == f.GetType())
                    .FirstOrDefault());
            }
            var result = await formatter.ReadRequestBodyAsync(context, encoding);
            return result;
        }
    }

    internal class MySuperJsonOutputFormatter : TextOutputFormatter
    {
        public MySuperJsonOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var mvcOpt = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
            var formatters = mvcOpt.OutputFormatters;
            TextOutputFormatter? formatter = null;

            Endpoint endpoint = context.HttpContext.GetEndpoint();
            if (endpoint.Metadata.GetMetadata<UseSpanJsonAttribute>() != null)
            {
                formatter = formatters.OfType<SpanJsonOutputFormatter<SpanJsonDefaultResolver<byte>>>().FirstOrDefault();
            }
            else
            {
                formatter = (NewtonsoftJsonOutputFormatter)(formatters
                    .Where(f => typeof(NewtonsoftJsonOutputFormatter) == f.GetType())
                    .FirstOrDefault());
            }
            await formatter.WriteResponseBodyAsync(context, selectedEncoding);
        }
    }
}
