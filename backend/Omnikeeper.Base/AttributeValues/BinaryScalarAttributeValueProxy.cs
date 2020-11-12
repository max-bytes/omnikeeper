using System;
using System.Security.Cryptography;

namespace Omnikeeper.Entity.AttributeValues
{
    public class BinaryScalarAttributeValueProxy
    {
        public bool HasFullData() => FullData != null;
        public byte[]? FullData { get; }
        public int FullSize { get; }
        public byte[] Sha256Hash { get; }
        public string MimeType { get; }
        private readonly string hashString;

        private BinaryScalarAttributeValueProxy(byte[] sha256Hash, string mimeType, int fullSize, byte[]? fullData)
        {
            Sha256Hash = sha256Hash;
            FullSize = fullSize;
            FullData = fullData;
            hashString = Convert.ToBase64String(sha256Hash);
            MimeType = mimeType;
        }

        private bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is BinaryScalarAttributeValueProxy proxy &&
                   AreEqual(Sha256Hash, proxy.Sha256Hash) &&
                   FullSize == proxy.FullSize &&
                   MimeType.Equals(proxy.MimeType);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Sha256Hash, FullSize, MimeType);
        }

        public override string ToString()
        {
            return hashString;
        }


        public static BinaryScalarAttributeValueProxy BuildFromHash(byte[] hash, string mimeType, int fullSize)
        {
            if (hash.Length != 32)
                throw new Exception("Invalid hash length");

            return new BinaryScalarAttributeValueProxy(hash, mimeType, fullSize, null);
        }
        public static BinaryScalarAttributeValueProxy BuildFromHashAndFullData(byte[] hash, string mimeType, int fullSize, byte[] fullData)
        {
            if (hash.Length != 32)
                throw new Exception("Invalid hash length");
            if (fullSize != fullData.Length)
                throw new Exception("Invalid length for full data");

            return new BinaryScalarAttributeValueProxy(hash, mimeType, fullSize, fullData);
        }
        public static BinaryScalarAttributeValueProxy BuildFromFullData(string mimeType, byte[] fullData)
        {
            var fullSize = fullData.Length;
            using SHA256 sha256Hash = SHA256.Create();
            var hash = sha256Hash.ComputeHash(fullData);
            return new BinaryScalarAttributeValueProxy(hash, mimeType, fullSize, fullData);
        }
    }
}
