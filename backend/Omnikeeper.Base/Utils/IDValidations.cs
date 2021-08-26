using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Utils
{
    public static class IDValidations
    {
        private static Regex LayerIDRegex = new Regex("^[a-z0-9_]+$");
        public static Regex PredicateIDRegex = new Regex("^[a-z0-9_.]+$");
        public static Regex TraitIDRegex = new Regex("^[a-z0-9_.]+$");
        public static Regex GridViewContextIDRegex = new Regex("^[a-z0-9_]+$");

        public static bool ValidateLayerID(string candidateID)
        {
            return LayerIDRegex.IsMatch(candidateID);
        }

        public static bool ValidatePredicateID(string candidateID)
        {
            return PredicateIDRegex.IsMatch(candidateID);
        }

        public static bool ValidateTraitID(string candidateID)
        {
            return TraitIDRegex.IsMatch(candidateID);
        }

        public static bool ValidateGridViewContextID(string candidateID)
        {
            return GridViewContextIDRegex.IsMatch(candidateID);
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

        public static void ValidateTraitIDThrow(string candidateID)
        {
            if (!ValidateTraitID(candidateID))
                throw new Exception($"Invalid trait ID \"{candidateID}\"");
        }


        public static void ValidateGridViewContextIDThrow(string candidateID)
        {
            if (!ValidateGridViewContextID(candidateID))
                throw new Exception($"Invalid gridview context ID \"{candidateID}\"");
        }
    }
}
