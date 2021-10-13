using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Utils
{
    public static class IDValidations
    {
        private static Regex LayerIDRegex = new Regex("^[a-z0-9_]+$");

        public const string PredicateIDRegexString = "^[a-z0-9_.]+$";
        public const RegexOptions PredicateIDRegexOptions = RegexOptions.None;
        public static Regex PredicateIDRegex = new Regex(PredicateIDRegexString, PredicateIDRegexOptions);

        public const string TraitIDRegexString = "^[a-z0-9_.]+$";
        public const RegexOptions TraitIDRegexOptions = RegexOptions.None;
        public static Regex TraitIDRegex = new Regex(TraitIDRegexString, TraitIDRegexOptions);

        private static Regex GeneratorIDRegex = new Regex("^[a-z0-9_.]+$");
        private static Regex CLConfigIDRegex = new Regex("^[a-z0-9_.]+$");


        public const string GridViewContextIDRegexString = "^[a-z0-9_]+$";
        public const RegexOptions GridViewContextIDRegexOptions = RegexOptions.None;
        public static Regex GridViewContextIDRegex = new Regex(GridViewContextIDRegexString, GridViewContextIDRegexOptions);

        public static bool ValidateLayerID(string candidateID)
        {
            return LayerIDRegex.IsMatch(candidateID);
        }

        public static bool ValidatePredicateID(string candidateID)
        {
            return PredicateIDRegex.IsMatch(candidateID);
        }

        public static bool ValidateGeneratorID(string candidateID)
        {
            return GeneratorIDRegex.IsMatch(candidateID);
        }

        public static bool ValidateCLConfigID(string candidateID)
        {
            return CLConfigIDRegex.IsMatch(candidateID);
        }

        public static void ValidateLayerIDThrow(string candidateID)
        {
            if (!ValidateLayerID(candidateID))
                throw new Exception($"Invalid layer ID \"{candidateID}\"");
        }
        public static void ValidateLayerIDsThrow(IEnumerable<string> candidateIDs)
        {
            foreach (var candidateID in candidateIDs)
                ValidateLayerIDThrow(candidateID);
        }

        public static void ValidatePredicateIDThrow(string candidateID)
        {
            if (!ValidatePredicateID(candidateID))
                throw new Exception($"Invalid predicate ID \"{candidateID}\"");
        }

        public static void ValidateGeneratorIDThrow(string candidateID)
        {
            if (!ValidateGeneratorID(candidateID))
                throw new Exception($"Invalid generator ID \"{candidateID}\"");
        }

        public static void ValidateCLConfigIDThrow(string candidateID)
        {
            if (!ValidateCLConfigID(candidateID))
                throw new Exception($"Invalid CL Config ID \"{candidateID}\"");
        }
    }
}
