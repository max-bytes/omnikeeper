﻿using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omnikeeper.Model
{
    public partial class BaseAttributeModel
    {
        private IAttributeValue Unmarshal(string valueText, byte[] valueBinary, byte[] valueControl, AttributeValueType type, bool fullBinary)
        {
            if (valueControl.Length == 0)
            { // V1 TODO: remove once no longer used
                var multiplicityIndicator = valueText.Substring(0, 1);
                var finalValue = valueText.Substring(1);
                if (multiplicityIndicator == "A")
                {
                    var tokenized = finalValue.Tokenize(',', '\\');
                    var finalValues = tokenized.Select(v => v.Replace("\\\\", "\\")).ToArray();
                    return type switch
                    {
                        AttributeValueType.Text => AttributeArrayValueText.BuildFromString(finalValues, false),
                        AttributeValueType.MultilineText => AttributeArrayValueText.BuildFromString(finalValues, true),
                        AttributeValueType.Integer => AttributeArrayValueInteger.BuildFromString(finalValues),
                        AttributeValueType.JSON => AttributeArrayValueJSON.BuildFromString(finalValues),
                        AttributeValueType.YAML => AttributeArrayValueYAML.BuildFromString(finalValues),
                        _ => throw new Exception($"Unknown type {type} encountered"),
                    };
                }
                else
                {
                    return type switch
                    {
                        AttributeValueType.Text => AttributeScalarValueText.BuildFromString(finalValue, false),
                        AttributeValueType.MultilineText => AttributeScalarValueText.BuildFromString(finalValue, true),
                        AttributeValueType.Integer => AttributeScalarValueInteger.BuildFromString(finalValue),
                        AttributeValueType.JSON => AttributeScalarValueJSON.BuildFromString(finalValue),
                        AttributeValueType.YAML => AttributeScalarValueYAML.BuildFromString(finalValue),
                        _ => throw new Exception($"Unknown type {type} encountered"),
                    };
                }
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

        private IAttributeValue UnmarshalNonV1(string valueText, byte[] valueBinary, byte[] valueControl, AttributeValueType type, bool fullBinary)
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
                                return AttributeScalarValueText.BuildFromString(UnmarshalStringV2(valueText, valueControl), multiline);
                        }
                    case AttributeValueType.Integer:
                        {
                            if (isArray)
                                return AttributeArrayValueInteger.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl));
                            else
                                return AttributeScalarValueInteger.BuildFromString(UnmarshalStringV2(valueText, valueControl));
                        }
                    case AttributeValueType.JSON:
                        {
                            if (isArray)
                                return AttributeArrayValueJSON.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl));
                            else
                                return AttributeScalarValueJSON.BuildFromString(UnmarshalStringV2(valueText, valueControl));
                        }
                    case AttributeValueType.YAML:
                        {
                            if (isArray)
                                return AttributeArrayValueYAML.BuildFromString(UnmarshalStringArrayV2(valueText, valueControl));
                            else
                                return AttributeScalarValueYAML.BuildFromString(UnmarshalStringV2(valueText, valueControl));
                        }
                    case AttributeValueType.Image:
                        {
                            if (fullBinary)
                            {
                                if (isArray)
                                    return AttributeArrayValueImage.Build(UnmarshalFullBinaryArrayV2(valueBinary, valueControl));
                                else
                                    return AttributeScalarValueImage.Build(UnmarshalFullBinaryV2(valueBinary, valueControl));
                            }
                            else
                            {
                                if (isArray)
                                    return AttributeArrayValueImage.Build(UnmarshalProxyBinaryArrayV2(valueControl));
                                else
                                    return AttributeScalarValueImage.Build(UnmarshalProxyBinaryV2(valueControl));
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

        private (string text, byte[] binary, byte[] control) Marshal(IAttributeValue value)
        {
            byte version = 0x02;
            if (version == 0x01)
            { // V1
              // HACK: first converting to DTO here is not very clean, but a DTO contains the value(s) as suitable-for-database string(s)
              // TODO: once V1 is no longer used, remove

                var vdto = AttributeValueDTO.Build(value);
                if (vdto.IsArray)
                {
                    var marshalled = string.Join(",", vdto.Values.Select(value => value.Replace("\\", "\\\\").Replace(",", "\\,")));
                    return ($"A{marshalled}", new byte[0], new byte[0]);
                }
                else
                {
                    return ($"S{vdto.Values[0]}", new byte[0], new byte[0]);
                }
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

        private (string valueText, byte[] valueBinary, byte[] valueControl) MarshalV2(IAttributeValue av)
        {
            return av switch
            {
                AttributeScalarValueText a => MarshalStringV2(a.Value),
                AttributeArrayValueText a => MarshalStringArrayV2(a.Values.Select(v => v.Value)),
                AttributeScalarValueInteger a => MarshalStringV2(a.Value.ToString()),
                AttributeArrayValueInteger a => MarshalStringArrayV2(a.Values.Select(v => v.Value.ToString())),
                AttributeScalarValueJSON a => MarshalStringV2(a.Value.ToString()),
                AttributeArrayValueJSON a => MarshalStringArrayV2(a.Values.Select(v => v.Value.ToString())),
                AttributeScalarValueYAML a => MarshalStringV2(a.Value.ToString()),
                AttributeArrayValueYAML a => MarshalStringArrayV2(a.Values.Select(v => v.Value.ToString())),
                AttributeScalarValueImage a => MarshalBinaryV2(a.Value),
                AttributeArrayValueImage a => MarshalBinaryArrayV2(a.Values.Select(v => v.Value)),

                _ => throw new Exception("Unknown IAttributeValue type encountered when trying to marshal")
            };
        }

        private (string valueText, byte[] valueBinary, byte[] valueControl) MarshalStringArrayV2(IEnumerable<string> values)
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
            return (marshalled, new byte[0], control);
        }

        private (string valueText, byte[] valueBinary, byte[] valueControl) MarshalStringV2(string value)
        {
            var controlHeader = new byte[]
            {
                0x02, // version
                0x01, // scalar
            };
            var control = controlHeader;
            return (value, new byte[0], control);
        }

        private (string valueText, byte[] valueBinary, byte[] valueControl) MarshalBinaryArrayV2(IEnumerable<BinaryScalarAttributeValueProxy> values)
        {
            if (values.Any(v => !v.HasFullData()))
                throw new Exception("Cannot marshal binary attribute value that does not contain the full data");
            var hashes = values.Select(v => v.Sha256Hash);
            if (hashes.Any(h => h.Length != 32))
                throw new Exception("Hash of invalid length for binary attribute value encountered");
            var mimeTypeBytes = values.Select(v => Encoding.UTF8.GetBytes(v.MimeType)).ToArray();
            var marshalled = values.SelectMany(v => v.FullData).ToArray();
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

        private (string valueText, byte[] valueBinary, byte[] valueControl) MarshalBinaryV2(BinaryScalarAttributeValueProxy value)
        {
            if (!value.HasFullData())
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

        private string[] UnmarshalStringArrayV2(string valueText, byte[] valueControl)
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

        private string UnmarshalStringV2(string valueText, byte[] valueControl)
        {
            return valueText;
        }

        private (byte[] elementHash, int elementSize, string mimeType)[] UnmarshalValueControlArrayV2(byte[] valueControl)
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
        private (byte[] elementHash, int elementSize, string mimeType) UnmarshalValueControlV2(byte[] valueControl)
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

        private IEnumerable<BinaryScalarAttributeValueProxy> UnmarshalProxyBinaryArrayV2(byte[] valueControl)
        {
            var valueControlArray = UnmarshalValueControlArrayV2(valueControl);
            return valueControlArray.Select(i => BinaryScalarAttributeValueProxy.BuildFromHash(i.elementHash, i.mimeType, i.elementSize));
        }
        private BinaryScalarAttributeValueProxy UnmarshalProxyBinaryV2(byte[] valueControl)
        {
            var (hash, size, mimeType) = UnmarshalValueControlV2(valueControl);
            return BinaryScalarAttributeValueProxy.BuildFromHash(hash, mimeType, size);
        }

        private IEnumerable<BinaryScalarAttributeValueProxy> UnmarshalFullBinaryArrayV2(byte[] valueBinary, byte[] valueControl)
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
        private BinaryScalarAttributeValueProxy UnmarshalFullBinaryV2(byte[] valueBinary, byte[] valueControl)
        {
            var (hash, size, mimeType) = UnmarshalValueControlV2(valueControl);
            return BinaryScalarAttributeValueProxy.BuildFromHashAndFullData(hash, mimeType, size, valueBinary);
        }

        // NOTE: we assume little-endian (BitConverter.IsLittleEndian)
        private byte[] Int2bytes(int number)
        {
            return BitConverter.GetBytes(number);
        }
        private int Bytes2int(byte[] b, int startIndex)
        {
            return BitConverter.ToInt32(b, startIndex);
        }

    }
}
