using SpanJson;
using SpanJson.Formatters.Dynamic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Omnikeeper.Base.Utils
{
    public sealed class InputsFormatter : ICustomJsonFormatter<Dictionary<string, object>>
    {
        public static readonly InputsFormatter Default = new InputsFormatter();

        public object Arguments { get; set; }

        public Dictionary<string, object> Deserialize(ref JsonReader<byte> reader)
        {
            return ReadDictionary(ref reader, null);
        }

        private Dictionary<string, object> ReadDictionary(ref JsonReader<byte> reader, JsonToken? nextToken)
        {
            JsonToken token = reader.ReadUtf8NextToken();

            if (token == JsonToken.Null)
                return null;

            reader.ReadBeginObjectOrThrow();

            var result = new Dictionary<string, object>();

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
                    return ReadDictionary(ref reader, null);
                case JsonToken.String:
                    return reader.ReadUtf8String();
                case JsonToken.True:
                case JsonToken.False:
                    return reader.ReadUtf8Boolean();
                case JsonToken.Null:
                    reader.ReadUtf8Null();
                    return null;
                case JsonToken.Number:
                    return new SpanJsonDynamicUtf8Number(reader.ReadNumberSpan());
                default:
                    throw new NotImplementedException();
            }

            //return token switch
            //   {
            //       JsonToken.BeginArray => ReadArray(ref reader),
            //       JsonToken.BeginObject => ReadDictionary(ref reader, null),
            //       JsonToken.String => reader.ReadUtf8String(),
            //       JsonToken.True => reader.ReadUtf8Boolean(),
            //       JsonToken.False => reader.ReadUtf8Boolean(),
            //       JsonToken.Null => reader.ReadUtf8Null(),
            //       JsonToken.Number => new SpanJsonDynamicUtf8Number(reader.ReadNumberSpan()),
            //       //_ => reader.ReadUtf8Dynamic()
            //       _ => throw new NotImplementedException()
            //   };
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
