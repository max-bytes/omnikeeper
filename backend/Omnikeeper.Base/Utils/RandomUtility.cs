using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omnikeeper.Base.Utils
{
    public static class RandomUtility
    {
        public static V GetRandom<V>(Random r, params (V item, int chance)[] possibilities)
        {
            var indices = possibilities.SelectMany((p, i) => Enumerable.Repeat(i, p.chance)).ToArray();
            var index = r.Next(0, indices.Length - 1);
            return possibilities[indices[index]].item;
        }

        // NOTE: only suitable for taking small numbers from larger collections, very slow otherwise
        public static IEnumerable<T> TakeRandom<T>(IList<T> collection, int take, Random random)
        {
            if (take > collection.Count)
                throw new Exception("Cannot take more than is there");
            var takenIndices = new HashSet<int>();
            for(var i = 0;i < take;i++)
            {
                int tryIndex;
                do
                {
                    tryIndex = random.Next(0, collection.Count);
                } while (takenIndices.Contains(tryIndex));

                takenIndices.Add(tryIndex);
                yield return collection[tryIndex];
            }
        }

        public static string GenerateRandomString(int length, Random random, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be less than zero.");
            if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

            const int byteSize = 0x100;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (byteSize < allowedCharSet.Length) throw new ArgumentException(String.Format("allowedChars may contain no more than {0} characters.", byteSize));

            // Guid.NewGuid and System.Random are not particularly random. By using a
            // cryptographically-secure random number generator, the caller is always
            // protected, regardless of use.
            //using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var result = new StringBuilder();
                var buf = new byte[128];
                while (result.Length < length)
                {
                    random.NextBytes(buf);
                    for (var i = 0; i < buf.Length && result.Length < length; ++i)
                    {
                        // Divide the byte into allowedCharSet-sized groups. If the
                        // random value falls into the last group and the last group is
                        // too small to choose from the entire allowedCharSet, ignore
                        // the value in order to avoid biasing the result.
                        var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                        if (outOfRangeStart <= buf[i]) continue;
                        result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                    }
                }
                return result.ToString();
            }
        }
    }
}
