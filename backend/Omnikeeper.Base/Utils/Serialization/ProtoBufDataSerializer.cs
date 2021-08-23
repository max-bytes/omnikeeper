using Omnikeeper.Base.Entity;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Omnikeeper.Base.Utils.Serialization
{
    public class ProtoBufDataSerializer : IDataSerializer
    {
        public ProtoBufDataSerializer()
        {
            if (!RuntimeTypeModel.Default.IsDefined(typeof(Color)))
                RuntimeTypeModel.Default.Add(typeof(Color), false).SetSurrogate(typeof(ColorSurrogate));
            if (!RuntimeTypeModel.Default.IsDefined(typeof(DateTimeOffset)))
                RuntimeTypeModel.Default.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate));
        }

        private readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
        public byte[] ToByteArray(object obj)
        {
            using MemoryStream memoryStream = new MemoryStream();
            if (obj is OIAContext o) // NOTE: fallback to binaryFormatter, because protobuf cannot serialize this class well
                binaryFormatter.Serialize(memoryStream, obj);
            else if (obj is IEnumerable<OIAContext> o2)
                Serializer.Serialize(memoryStream, o2);
            else
                Serializer.Serialize(memoryStream, obj);
            var a = memoryStream.ToArray();
            return a;
        }
        public T? FromByteArray<T>(byte[] byteArray) where T : class
        {
            using MemoryStream memoryStream = new MemoryStream(byteArray);
            if (typeof(T) == typeof(OIAContext) || typeof(T) == typeof(IEnumerable<OIAContext>))
                return binaryFormatter.Deserialize(memoryStream) as T;
            else
                return Serializer.Deserialize<T>(memoryStream);
        }

        [ProtoContract]
        public class DateTimeOffsetSurrogate
        {
            [ProtoMember(1)]
            public string DateTimeString { get; set; } = "";

            public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset value)
            {
                return new DateTimeOffsetSurrogate { DateTimeString = value.ToString("o") };
            }

            public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate value)
            {
                try
                {
                    return DateTimeOffset.Parse(value.DateTimeString, null, DateTimeStyles.RoundtripKind);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to parse date time value: " + value.DateTimeString, ex);
                }
            }
        }

        [ProtoContract]
        public class ColorSurrogate
        {
            [ProtoMember(1)]
            public int Key { get; set; }

            public static implicit operator Color(ColorSurrogate surrogate)
            {
                return surrogate == null ? Color.Empty : Color.FromArgb(surrogate.Key);
            }

            public static implicit operator ColorSurrogate(Color source)
            {
                var key = source == default ? Color.Empty.ToArgb() : source.ToArgb();
                return new ColorSurrogate { Key = key };
            }
        }
    }
}
