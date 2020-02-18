using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Tests.Integration.GraphQL
{
    public static class AssertionExtensions
    {
        /// <summary>
        /// Compares two strings after normalizing line breaks.
        /// </summary>
        /// <param name="actual">Actual value.</param>
        /// <param name="expected">Expected value.</param>
        /// <param name="customMessage">A custom message if they aren't the same.</param>
        public static void ShouldBeCrossPlat(this string actual, string expected, string customMessage)
            => Assert.AreEqual(Normalize(expected), Normalize(actual), customMessage);

        /// <summary>
        /// Compares two strings after normalizing line breaks.
        /// </summary>
        /// <param name="actual">Actual value.</param>
        /// <param name="expected">Expected value.</param>
        public static void ShouldBeCrossPlat(this string actual, string expected)
            => Assert.AreEqual(Normalize(expected), Normalize(actual));

        /// <summary>
        /// Compares two JSON strings after normalizing line breaks and parsing then writing them back out
        /// to ignore any whitespace differences.
        /// </summary>
        /// <param name="actualJson">Actual value.</param>
        /// <param name="expectedJson">Expected value.</param>
        public static void ShouldBeCrossPlatJson(this string actualJson, string expectedJson)
        {
            using var actualJsonDoc = JsonDocument.Parse(Normalize(actualJson));
            using var expectedJsonDoc = JsonDocument.Parse(Normalize(expectedJson));
            Assert.AreEqual(JsonSerializer.Serialize(expectedJsonDoc.RootElement), JsonSerializer.Serialize(actualJsonDoc.RootElement));
        }

        private static string Normalize(this string value) => value?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
    }
}
