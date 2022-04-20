using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Startup
{
    // System.Text.Json does not support serializing Exceptions, so we need a custom converter
    // see https://github.com/dotnet/runtime/issues/43026#issuecomment-705904399
    public class ExceptionConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(Exception));
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(ExceptionConverterInner<>).MakeGenericType(new Type[] { type }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: Array.Empty<object>(),
                culture: null)!;

            return converter;
        }

        class ExceptionConverterInner<TException> : JsonConverter<TException> where TException : Exception
        {
            public override TException Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, TException value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("Message", value.Message);
                writer.WriteEndObject();
            }
        }
    }
}
