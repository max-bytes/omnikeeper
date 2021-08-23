using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Omnikeeper.Base.Utils.Serialization
{
    public class BinaryFormatterDataSerializer : IDataSerializer
    {
        private readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
        public byte[] ToByteArray(object obj)
        {
            using MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, obj);
            return memoryStream.ToArray();
        }

        public T? FromByteArray<T>(byte[] byteArray) where T : class
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using MemoryStream memoryStream = new MemoryStream(byteArray);
            return binaryFormatter.Deserialize(memoryStream) as T;
        }
    }
}
