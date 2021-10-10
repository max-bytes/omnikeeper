using SpanJson;
using SpanJson.Formatters.Dynamic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Omnikeeper.Base.Utils
{
    public sealed class GraphQLInputsFormatter : ICustomJsonFormatter<Dictionary<string, object>>
    {
        public static readonly GraphQLInputsFormatter Default = new GraphQLInputsFormatter();

        public object? Arguments { get; set; }

        public Dictionary<string, object> Deserialize(ref JsonReader<byte> reader)
        {
            return ReadDictionary(ref reader);
        }

        private Dictionary<string, object> ReadDictionary(ref JsonReader<byte> reader)
        {
            JsonToken token = reader.ReadUtf8NextToken();

            var result = new Dictionary<string, object>();

            if (token == JsonToken.Null)
            {
                reader.ReadUtf8Null();
                return result;
            }

            reader.ReadBeginObjectOrThrow();

            var separated = false;
            while (reader.ReadUtf8NextToken() != JsonToken.EndObject)
            {
                if (separated) reader.SkipNextUtf8Value(JsonToken.ValueSeparator);

                string key = reader.ReadUtf8EscapedName();

                result.Add(key, ReadValue(ref reader));

                separated = true;
            }

            reader.ReadEndObjectOrThrow();

            return result;
        }

        private object ReadValue(ref JsonReader<byte> reader)
        {
            var token = reader.ReadUtf8NextToken();

            switch (token)
            {
                case JsonToken.BeginArray:
                    return ReadArray(ref reader);
                case JsonToken.BeginObject:
                    return ReadDictionary(ref reader);
                case JsonToken.String:
                    return reader.ReadUtf8String();
                case JsonToken.True:
                case JsonToken.False:
                    return reader.ReadUtf8Boolean();
                case JsonToken.Null:
                    reader.ReadUtf8Null();
                    return null!;
                case JsonToken.Number:
                    var span = reader.ReadNumberSpan();
                    var dynamicNumber = new SpanJsonDynamicUtf8Number(span);
                    if (dynamicNumber.TryConvert(typeof(int), out var i))
                    {
                        return i;
                    } 
                    else if (dynamicNumber.TryConvert(typeof(float), out var f))
                    {
                        return f;
                    }
                    else if (dynamicNumber.TryConvert(typeof(double), out var d))
                    {
                        return d;
                    } else
                    {
                        throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private List<object> ReadArray(ref JsonReader<byte> reader)
        {
            var result = new List<object>();

            reader.ReadUtf8BeginArrayOrThrow();

            var separated = false;
            while (reader.ReadUtf8NextToken() != JsonToken.EndArray)
            {
                if (separated) reader.SkipNextUtf8Value(JsonToken.ValueSeparator);
                result.Add(ReadValue(ref reader));
                separated = true;
            }

            reader.ReadUtf8EndArrayOrThrow();

            return result;
        }

        public Dictionary<string, object> Deserialize(ref JsonReader<char> reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref JsonWriter<byte> writer, Dictionary<string, object> value)
        {
            throw new NotImplementedException();
        }

        public void Serialize(ref JsonWriter<char> writer, Dictionary<string, object> value)
        {
            throw new NotImplementedException();
        }
    }
}
