using System;
using System.Security.Cryptography;

namespace Omnikeeper.Entity.AttributeValues
{
    //[ProtoContract(SkipConstructor = true)]
    public class BinaryScalarAttributeValueProxy
    {
        public bool HasFullData() => FullData != null;

        //[ProtoMember(1)] 
        private readonly byte[]? fullData;
        public byte[]? FullData => fullData;
        //[ProtoMember(2)] 
        private readonly int fullSize;
        public int FullSize => fullSize;
        //[ProtoMember(3)] 
        private readonly byte[] sha256Hash;
        public byte[] Sha256Hash => sha256Hash;
        //[ProtoMember(4)] 
        private readonly string mimeType;
        public string MimeType => mimeType;

        private BinaryScalarAttributeValueProxy(byte[] sha256Hash, string mimeType, int fullSize, byte[]? fullData)
        {
            this.sha256Hash = sha256Hash;
            this.fullSize = fullSize;
            this.fullData = fullData;
            this.mimeType = mimeType;
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
            return Convert.ToBase64String(sha256Hash);
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
