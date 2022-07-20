using GraphQL.Types;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Omnikeeper.Base.AttributeValues
{
    public static class AttributeValueHelper
    {
        public static IAttributeValue BuildFromTypeAndObject(AttributeValueType type, object o)
        {
            try
            {
                switch (type)
                {
                    case AttributeValueType.Text:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be string, found null");
                            else if (o is string str)
                                return new AttributeScalarValueText(str, false);
                            else if (o is string[] strArray)
                            {
                                return AttributeArrayValueText.BuildFromString(strArray, false);
                            }
                            else
                                throw new Exception($"Expected object {o} to be string, found {o.GetType().Name}");
                        }
                    case AttributeValueType.MultilineText:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be string, found null");
                            else if (o is string str)
                                return new AttributeScalarValueText(str, true);
                            else if (o is string[] strArray)
                            {
                                return AttributeArrayValueText.BuildFromString(strArray, true);
                            }
                            else
                                throw new Exception($"Expected object {o} to be string, found {o.GetType().Name}");
                        }
                    case AttributeValueType.Integer:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be long, found null");
                            else if (o is long lo)
                                return new AttributeScalarValueInteger(lo);
                            else if (o is long[] loa)
                                return AttributeArrayValueInteger.Build(loa);
                            else
                                throw new Exception($"Expected object {o} to be long, found {o.GetType().Name}");
                        }
                    case AttributeValueType.Double:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be double, found null");
                            else if (o is double[] da)
                                return AttributeArrayValueDouble.Build(da);
                            else if (o is double d)
                                return new AttributeScalarValueDouble(d);
                            else
                                throw new Exception($"Expected object {o} to be double, found {o.GetType().Name}");
                        }
                    case AttributeValueType.Boolean:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be boolean, found null");
                            else if (o is bool[] da)
                                return AttributeArrayValueBoolean.Build(da);
                            else if (o is bool d)
                                return new AttributeScalarValueBoolean(d);
                            else
                                throw new Exception($"Expected object {o} to be boolean, found {o.GetType().Name}");
                        }
                    case AttributeValueType.DateTimeWithOffset:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be DateTimeWithOffset, found null");
                            else if (o is DateTimeOffset[] da)
                                return AttributeArrayValueDateTimeWithOffset.Build(da);
                            else if (o is DateTimeOffset d)
                                return new AttributeScalarValueDateTimeWithOffset(d);
                            else
                                throw new Exception($"Expected object {o} to be DateTimeWithOffset, found {o.GetType().Name}");
                        }
                    case AttributeValueType.JSON:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to not be null, found null");
                            else if (o is object[] oa)
                            {
                                if (o is JsonDocument[] ja)
                                    return AttributeArrayValueJSON.BuildFromJsonDocuments(ja);
                                else
                                {
                                    // NOTE: ideally, we would have liked to just typecheck for string[], but that does not 
                                    // treat object[] with string elements correctly
                                    var stringArray = new string[oa.Length];
                                    for (var i = 0; i < oa.Length; i++)
                                    {
                                        if (oa[i] is not string s)
                                            throw new Exception($"Cannot deal with object {o}, that is an array, but neither string array nor JsonDocument array");
                                        stringArray[i] = s;
                                    }
                                    return AttributeArrayValueJSON.BuildFromString(stringArray, true);
                                }
                            }
                            else
                            {
                                if (o is JsonDocument t)
                                    return AttributeScalarValueJSON.BuildFromJsonDocument(t);
                                else if (o is string so)
                                    return AttributeScalarValueJSON.BuildFromString(so, true);
                                else
                                {
                                    throw new Exception($"Expected object {o} to be JsonDocument or string, found {o.GetType().Name}");
                                }
                            }
                        }
                    case AttributeValueType.YAML:
                        {
                            if (o == null)
                                throw new Exception($"Expected object {o} to be string, found null");
                            else if (o is string[] oa)
                                return AttributeArrayValueYAML.BuildFromString(oa);
                            else if (o is string t)
                                return AttributeScalarValueYAML.BuildFromString(t);
                            else
                                throw new Exception($"Expected object {o} to be YAML, found {o.GetType().Name}");
                        }
                    case AttributeValueType.Mask:
                        return AttributeScalarValueMask.Instance;
                    case AttributeValueType.Image:
                        {
                            throw new Exception("Building AttributeValueImage from type and object not allowed");
                        }
                    default:
                        throw new Exception($"Unknown type {type} encountered");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not build attribute value of type {type} from object {o}", e);
            }
        }

        public static IAttributeValue? BuildFromTypeAndJsonElement(AttributeValueType type, ref JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined)
                return null;

            switch (type)
            {
                case AttributeValueType.Text:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueText.BuildFromString(el.EnumerateArray().Select(e => e.ToString()!));
                            case JsonValueKind.String:
                            case JsonValueKind.Number:
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                return new AttributeScalarValueText(el.ToString()!);
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to string, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.MultilineText:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueText.BuildFromString(el.EnumerateArray().Select(e => e.ToString()!), true);
                            case JsonValueKind.String:
                            case JsonValueKind.Number:
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                return new AttributeScalarValueText(el.ToString()!, true);
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to string, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.Integer:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueInteger.Build(el.EnumerateArray().Select(e => e.GetInt64()!).ToArray());
                            case JsonValueKind.Number:
                                return new AttributeScalarValueInteger(el.GetInt64());
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to long, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.Double:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueDouble.Build(el.EnumerateArray().Select(e => e.GetDouble()!).ToArray());
                            case JsonValueKind.Number:
                                return new AttributeScalarValueDouble(el.GetDouble());
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to double, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.Boolean:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueBoolean.Build(el.EnumerateArray().Select(e => e.GetBoolean()!).ToArray());
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                return new AttributeScalarValueBoolean(el.GetBoolean());
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to boolean, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.DateTimeWithOffset:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueDateTimeWithOffset.BuildFromString(el.EnumerateArray().Select(e => e.GetString()!).ToArray());
                            case JsonValueKind.String:
                                return AttributeScalarValueDateTimeWithOffset.BuildFromString(el.GetString()!);
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to DateTimeWithOffset, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.JSON:
                    {
                        return AttributeScalarValueJSON.BuildFromJsonElement(el);
                    }
                case AttributeValueType.YAML:
                    {
                        switch (el.ValueKind)
                        {
                            case JsonValueKind.Array:
                                return AttributeArrayValueYAML.BuildFromString(el.EnumerateArray().Select(e => e.ToString()!).ToArray());
                            case JsonValueKind.String:
                            case JsonValueKind.Number:
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                return new AttributeScalarValueText(el.ToString()!);
                            default:
                                throw new Exception($"Expected JsonElement {el} to be convertible to YAML, found {el.ValueKind}");
                        }
                    }
                case AttributeValueType.Mask:
                    return AttributeScalarValueMask.Instance;
                case AttributeValueType.Image:
                    {
                        throw new Exception("Building AttributeValueImage from type and JsonElement not allowed");
                    }
                default:
                    throw new Exception($"Unknown type {type} encountered");
            }
        }

        public static IAttributeValue BuildFromDTO(AttributeValueDTO generic)
        {
            if (generic.IsArray)
                return generic.Type switch
                {
                    AttributeValueType.Text => AttributeArrayValueText.BuildFromString(generic.Values, false),
                    AttributeValueType.MultilineText => AttributeArrayValueText.BuildFromString(generic.Values, true),
                    AttributeValueType.Integer => AttributeArrayValueInteger.BuildFromString(generic.Values),
                    AttributeValueType.Double => AttributeArrayValueDouble.BuildFromString(generic.Values),
                    AttributeValueType.Boolean => AttributeArrayValueBoolean.BuildFromString(generic.Values),
                    AttributeValueType.DateTimeWithOffset => AttributeArrayValueDateTimeWithOffset.BuildFromString(generic.Values),
                    AttributeValueType.JSON => AttributeArrayValueJSON.BuildFromString(generic.Values, true),
                    AttributeValueType.YAML => AttributeArrayValueYAML.BuildFromString(generic.Values),
                    AttributeValueType.Mask => AttributeScalarValueMask.Instance,
                    AttributeValueType.Image => throw new Exception("Building AttributeValueImage from DTO not allowed"),
                    _ => throw new Exception($"Unknown type {generic.Type} encountered"),
                };
            else
                return generic.Type switch
                {
                    AttributeValueType.Text => new AttributeScalarValueText(generic.Values[0], false),
                    AttributeValueType.MultilineText => new AttributeScalarValueText(generic.Values[0], true),
                    AttributeValueType.Integer => AttributeScalarValueInteger.BuildFromString(generic.Values[0]),
                    AttributeValueType.Double => AttributeScalarValueDouble.BuildFromString(generic.Values[0]),
                    AttributeValueType.Boolean => AttributeScalarValueBoolean.BuildFromString(generic.Values[0]),
                    AttributeValueType.DateTimeWithOffset => AttributeScalarValueDateTimeWithOffset.BuildFromString(generic.Values[0]),
                    AttributeValueType.JSON => AttributeScalarValueJSON.BuildFromString(generic.Values[0], true),
                    AttributeValueType.YAML => AttributeScalarValueYAML.BuildFromString(generic.Values[0]),
                    AttributeValueType.Mask => AttributeScalarValueMask.Instance,
                    AttributeValueType.Image => throw new Exception("Building AttributeValueImage from DTO not allowed"),
                    _ => throw new Exception($"Unknown type {generic.Type} encountered"),
                };
        }

        public static IGraphType AttributeValueType2GraphQLType(AttributeValueType type, bool isArray)
        {
            var graphType = type switch
            {
                AttributeValueType.Text => (IGraphType)new StringGraphType(),
                AttributeValueType.MultilineText => new StringGraphType(),
                AttributeValueType.Integer => new LongGraphType(),
                AttributeValueType.Double => new FloatGraphType(), // Note: GraphQL's Float type is actually double-precision and hence, a double
                AttributeValueType.Boolean => new BooleanGraphType(),
                AttributeValueType.DateTimeWithOffset => new DateTimeOffsetGraphType(),
                AttributeValueType.JSON => new StringGraphType(),
                AttributeValueType.YAML => new StringGraphType(),
                AttributeValueType.Image => new StringGraphType(),
                AttributeValueType.Mask => new StringGraphType(),
                _ => throw new NotImplementedException(),
            };
            if (isArray)
                graphType = new ListGraphType(graphType);

            return graphType;
        }

        public static IAttributeValue Unmarshal(string valueText, byte[] valueBinary, byte[] valueControl, AttributeValueType type, bool fullBinary)
        {
            if (valueControl.Length == 0)
            {
                throw new Exception("Version 0x01 not supported any longer");
            }
            else
            { // non-V1
                try
                {
                    return UnmarshalNonV1(valueText, valueBinary, valueControl, type, fullBinary);
                }
                catch (Exception e)
                {
                    throw new Exception("Could not parse attribute value", e);
                }
            }
        }

        private static IAttributeValue UnmarshalNonV1(string valueText, byte[] valueBinary, byte[] valueControl, AttributeValueType type, bool fullBinary)
        {
            var version = valueControl[0];
            var isArray = valueControl[1] == 0x02;

            if (version == 0x01)
            {
                throw new Exception("Did not expect attribute value to be stored in version 0x01");
            }
            else if (version == 0x02)
            {
                switch (type)
                {
                    case AttributeValueType.Text:
                    case AttributeValueType.MultilineText:
                        {
                            var multiline = type == AttributeValueType.MultilineText;
                            if (isArray)
                                return AttributeArrayValueText.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl), multiline);
                            else
                                return new AttributeScalarValueText(UnmarshalStringV2(valueText, valueControl), multiline);
                        }
                    case AttributeValueType.Integer:
                        {
                            if (isArray)
                                return AttributeArrayValueInteger.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl));
                            else
                                return AttributeScalarValueInteger.BuildFromString(UnmarshalStringV2(valueText, valueControl));
                        }
                    case AttributeValueType.Double:
                        {
                            if (isArray)
                                return AttributeArrayValueDouble.BuildFromBytes(UnmarshalSimpleBinaryArrayV2(valueBinary, valueControl));
                            else
                                return AttributeScalarValueDouble.BuildFromBytes(UnmarshalSimpleBinaryV2(valueBinary, valueControl));
                        }
                    case AttributeValueType.Boolean:
                        {
                            if (isArray)
                                return AttributeArrayValueBoolean.BuildFromBytes(UnmarshalSimpleBinaryArrayV2(valueBinary, valueControl));
                            else
                                return AttributeScalarValueBoolean.BuildFromBytes(UnmarshalSimpleBinaryV2(valueBinary, valueControl));
                        }
                    case AttributeValueType.DateTimeWithOffset:
                        {
                            if (isArray)
                                return AttributeArrayValueDateTimeWithOffset.BuildFromBytes(UnmarshalSimpleBinaryArrayV2(valueBinary, valueControl));
                            else
                                return AttributeScalarValueDateTimeWithOffset.BuildFromBytes(UnmarshalSimpleBinaryV2(valueBinary, valueControl));
                        }
                    case AttributeValueType.JSON:
                        {
                            if (isArray)
                                return AttributeArrayValueJSON.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl), false);
                            else
                                return AttributeScalarValueJSON.BuildFromString(UnmarshalStringV2(valueText, valueControl), false);
                        }
                    case AttributeValueType.YAML:
                        {
                            if (isArray)
                                return AttributeArrayValueYAML.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl));
                            else
                                return AttributeScalarValueYAML.BuildFromString(UnmarshalStringV2(valueText, valueControl));
                        }
                    case AttributeValueType.Mask:
                        {
                            return AttributeScalarValueMask.Instance;
                        }
                    case AttributeValueType.Image:
                        {
                            if (fullBinary)
                            {
                                if (isArray)
                                    return AttributeArrayValueImage.Build(UnmarshalComplexFullBinaryArrayV2(valueBinary, valueControl));
                                else
                                    return new AttributeScalarValueImage(UnmarshalComplexFullBinaryV2(valueBinary, valueControl));
                            }
                            else
                            {
                                if (isArray)
                                    return AttributeArrayValueImage.Build(UnmarshalComplexProxyBinaryArrayV2(valueControl));
                                else
                                    return new AttributeScalarValueImage(UnmarshalComplexProxyBinaryV2(valueControl));
                            }
                        }
                    default:
                        throw new Exception("Unknown AttributeValueType encountered when trying to unmarshal");
                }
            }
            else
            {
                throw new NotImplementedException("Unknown version for attribute value retrieval");
            }
        }

        public static string BuildSQLIsScalarCheckClause(string fieldNameContainingValueControl = "value_control")
        {
            return $"(NOT (get_byte({fieldNameContainingValueControl}, 0) = 2 AND get_byte({fieldNameContainingValueControl}, 1) = 2))";
        }

        public static (string text, byte[] binary, byte[] control) Marshal(IAttributeValue value)
        {
            byte version = 0x02;
            if (version == 0x01)
            { // V1
                throw new Exception("Version 0x01 not supported any longer");
            }
            else if (version == 0x02)
            { // V2
                return MarshalV2(value);
            }
            else
            {
                throw new NotImplementedException("Unknown version for attribute value storage");
            }
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalV2(IAttributeValue av)
        {
            return av switch
            {
                AttributeScalarValueText a => MarshalStringV2(a.Value),
                AttributeArrayValueText a => MarshalStringArrayV2(a.Values.Select(v => v.Value)),
                AttributeScalarValueInteger a => MarshalStringV2(a.Value.ToString()),
                AttributeArrayValueInteger a => MarshalStringArrayV2(a.Values.Select(v => v.Value.ToString())),
                AttributeScalarValueDouble a => MarshalSimpleBinaryV2(a.ToBytes()),
                AttributeArrayValueDouble a => MarshalSimpleBinaryArrayV2(a.Values.Select(v => v.ToBytes())),
                AttributeScalarValueBoolean a => MarshalSimpleBinaryV2(a.ToBytes()),
                AttributeArrayValueBoolean a => MarshalSimpleBinaryArrayV2(a.Values.Select(v => v.ToBytes())),
                AttributeScalarValueDateTimeWithOffset a => MarshalSimpleBinaryV2(a.ToBytes()),
                AttributeArrayValueDateTimeWithOffset a => MarshalSimpleBinaryArrayV2(a.Values.Select(v => v.ToBytes())),
                AttributeScalarValueJSON a => MarshalStringV2(a.Value2String()),
                AttributeArrayValueJSON a => MarshalStringArrayV2(a.Values.Select(v => v.Value2String())),
                AttributeScalarValueYAML a => MarshalStringV2((a.Value.ToString())!),
                AttributeArrayValueYAML a => MarshalStringArrayV2(a.Values.Select(v => (v.Value.ToString())!)),
                AttributeScalarValueMask a => MarshalStringV2(""),
                AttributeScalarValueImage a => MarshalComplexBinaryV2(a.Value),
                AttributeArrayValueImage a => MarshalComplexBinaryArrayV2(a.Values.Select(v => v.Value)),

                _ => throw new Exception("Unknown IAttributeValue type encountered when trying to marshal")
            };
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalStringArrayV2(IEnumerable<string> values)
        {
            var marshalled = string.Join("", values);
            var controlHeader = new byte[]
            {
                                0x02, // version
                                0x02, // array
            };
            var control = controlHeader
                .Concat(Int2bytes(values.Count()))
                .Concat(values.SelectMany(v => Int2bytes(v.Length)))
                .ToArray();
            return (marshalled, Array.Empty<byte>(), control);
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalStringV2(string value)
        {
            var controlHeader = new byte[]
            {
                0x02, // version
                0x01, // scalar
            };
            var control = controlHeader;
            return (value, Array.Empty<byte>(), control);
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalComplexBinaryArrayV2(IEnumerable<BinaryScalarAttributeValueProxy> values)
        {
            if (values.Any(v => !v.HasFullData()))
                throw new Exception("Cannot marshal binary attribute value that does not contain the full data");
            var hashes = values.Select(v => v.Sha256Hash);
            if (hashes.Any(h => h.Length != 32))
                throw new Exception("Hash of invalid length for binary attribute value encountered");
            var mimeTypeBytes = values.Select(v => Encoding.UTF8.GetBytes(v.MimeType)).ToArray();
            var marshalled = values.SelectMany(v => v.FullData ?? Array.Empty<byte>()).ToArray();
            var controlHeader = new byte[]
            {
                                0x02, // version
                                0x02, // array
            };
            var control = controlHeader
                .Concat(Int2bytes(values.Count()))
                .Concat(values.SelectMany(v => Int2bytes(v.FullSize)))
                .Concat(values.SelectMany(v => v.Sha256Hash))
                .Concat(mimeTypeBytes.SelectMany(v => Int2bytes(v.Length)))
                .Concat(mimeTypeBytes.SelectMany(v => v))
                .ToArray();
            return ("", marshalled, control);
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalComplexBinaryV2(BinaryScalarAttributeValueProxy value)
        {
            if (value.FullData == null)
                throw new Exception("Cannot marshal binary attribute value that does not contain the full data");
            var hash = value.Sha256Hash;
            if (hash.Length != 32)
                throw new Exception("Hash of invalid length for binary attribute value encountered");
            var mimeTypeBytes = Encoding.UTF8.GetBytes(value.MimeType);
            var controlHeader = new byte[]
            {
                0x02, // version
                0x01, // scalar
            };
            var control = controlHeader
                .Concat(Int2bytes(value.FullSize))
                .Concat(hash)
                .Concat(Int2bytes(mimeTypeBytes.Length))
                .Concat(mimeTypeBytes)
                .ToArray();
            return ("", value.FullData, control);
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalSimpleBinaryArrayV2(IEnumerable<byte[]> bytes)
        {
            var controlHeader = new byte[]
            {
                0x02, // version
                0x02, // array
            };
            var control = controlHeader
                .Concat(Int2bytes(bytes.Count()))
                .ToArray();
            return ("", bytes.SelectMany(t => t).ToArray(), control);
        }

        private static (string valueText, byte[] valueBinary, byte[] valueControl) MarshalSimpleBinaryV2(byte[] bytes)
        {
            var controlHeader = new byte[]
            {
                0x02, // version
                0x01, // scalar
            };
            var control = controlHeader;
            return ("", bytes, control);
        }

        private static string[] UnmarshalStringArrayV2(string valueText, byte[] valueControl)
        {
            var arrayLength = Bytes2int(valueControl, 2);
            var elementSizes = Enumerable.Range(0, arrayLength).Select(i => Bytes2int(valueControl, 2 + 4 + i * 4)).ToArray();

            var elements = new string[arrayLength];
            for (int i = 0, start = 0; i < arrayLength; i++)
            {
                var len = elementSizes[i];
                elements[i] = valueText.Substring(start, len);
                start += len;
            }
            return elements;
        }

        private static string UnmarshalStringV2(string valueText, byte[] valueControl)
        {
            return valueText;
        }

        private static (byte[] elementHash, int elementSize, string mimeType)[] UnmarshalValueControlArrayV2(byte[] valueControl)
        {
            var byteOffset = 2;
            var arrayLength = Bytes2int(valueControl, byteOffset);
            byteOffset += 4;
            var elementSizes = Enumerable.Range(0, arrayLength).Select(i => Bytes2int(valueControl, byteOffset + i * 4)).ToArray();
            byteOffset += arrayLength * 4;
            var elementHashes = Enumerable.Range(0, arrayLength).Select(i => new ArraySegment<byte>(valueControl, byteOffset + i * 32, 32).ToArray()).ToArray();
            byteOffset += arrayLength * 32;
            var mimeTypeLength = Enumerable.Range(0, arrayLength).Select(i => Bytes2int(valueControl, byteOffset + i * 4)).ToArray();
            byteOffset += arrayLength * 4;
            var mimeTypes = new string[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                var len = mimeTypeLength[i];
                mimeTypes[i] = Encoding.UTF8.GetString(valueControl, byteOffset, len);
                byteOffset += len;
            }
            var ret = new (byte[] elementHash, int elementSize, string mimeType)[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                ret[i] = (elementHashes[i], elementSizes[i], mimeTypes[i]);
            }
            return ret;
        }
        private static (byte[] elementHash, int elementSize, string mimeType) UnmarshalValueControlV2(byte[] valueControl)
        {
            var byteOffset = 2;
            var fullSize = Bytes2int(valueControl, byteOffset);
            byteOffset += 4;
            var hash = new ArraySegment<byte>(valueControl, byteOffset, 32).ToArray();
            byteOffset += 32;
            var mimeTypeLength = Bytes2int(valueControl, byteOffset);
            byteOffset += 4;
            var mimeType = Encoding.UTF8.GetString(valueControl, byteOffset, mimeTypeLength);
            return (hash, fullSize, mimeType);
        }

        private static IEnumerable<BinaryScalarAttributeValueProxy> UnmarshalComplexProxyBinaryArrayV2(byte[] valueControl)
        {
            var valueControlArray = UnmarshalValueControlArrayV2(valueControl);
            return valueControlArray.Select(i => BinaryScalarAttributeValueProxy.BuildFromHash(i.elementHash, i.mimeType, i.elementSize));
        }
        private static BinaryScalarAttributeValueProxy UnmarshalComplexProxyBinaryV2(byte[] valueControl)
        {
            var (hash, size, mimeType) = UnmarshalValueControlV2(valueControl);
            return BinaryScalarAttributeValueProxy.BuildFromHash(hash, mimeType, size);
        }

        private static IEnumerable<BinaryScalarAttributeValueProxy> UnmarshalComplexFullBinaryArrayV2(byte[] valueBinary, byte[] valueControl)
        {
            var valueControlArray = UnmarshalValueControlArrayV2(valueControl);
            var elements = new BinaryScalarAttributeValueProxy[valueControlArray.Length];
            for (int i = 0, start = 0; i < valueControlArray.Length; i++)
            {
                var fullSize = valueControlArray[i].elementSize;
                elements[i] = BinaryScalarAttributeValueProxy.BuildFromHashAndFullData(
                    valueControlArray[i].elementHash,
                    valueControlArray[i].mimeType,
                    fullSize,
                    new ArraySegment<byte>(valueBinary, start, fullSize).ToArray());
                start += fullSize;
            }
            return elements;
        }
        private static BinaryScalarAttributeValueProxy UnmarshalComplexFullBinaryV2(byte[] valueBinary, byte[] valueControl)
        {
            var (hash, size, mimeType) = UnmarshalValueControlV2(valueControl);
            return BinaryScalarAttributeValueProxy.BuildFromHashAndFullData(hash, mimeType, size, valueBinary);
        }

        private static byte[] UnmarshalSimpleBinaryArrayV2(byte[] valueBinary, byte[] valueControl)
        {
            return valueBinary;
        }

        private static byte[] UnmarshalSimpleBinaryV2(byte[] valueBinary, byte[] valueControl)
        {
            return valueBinary;
        }

        // NOTE: we assume little-endian (BitConverter.IsLittleEndian)
        private static byte[] Int2bytes(int number)
        {
            return BitConverter.GetBytes(number);
        }
        private static int Bytes2int(byte[] b, int startIndex)
        {
            return BitConverter.ToInt32(b, startIndex);
        }

    }
}
