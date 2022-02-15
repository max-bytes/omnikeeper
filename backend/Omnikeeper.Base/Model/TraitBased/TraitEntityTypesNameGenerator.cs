﻿using Omnikeeper.Base.Entity;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Model.TraitBased
{
    public static class TraitEntityTypesNameGenerator
    {
        private static readonly Regex AllowedNameRegex = new Regex("[^a-zA-Z0-9_]");
        public static string SanitizeTypeName(string unsanitizedTypeName)
        {
            var tmp = unsanitizedTypeName;
            tmp = tmp.Replace(".", "__");

            tmp = AllowedNameRegex.Replace(tmp, "");

            if (tmp.StartsWith("__"))
                tmp = "m" + tmp; // graphql does not support types starting with __, so we prefix it with an "m" (for meta)

            if (Regex.IsMatch(tmp, "^[0-9]"))
                tmp = "m" + tmp; // graphql does not support types starting with a digit, so we prefix it with an "m" (for meta)

            return tmp;
        }
        public static string SanitizeFieldName(string unsanitizedFieldName)
        {
            // NOTE: fields and types have same naming rules, so we can re-use
            return SanitizeTypeName(unsanitizedFieldName);
        }
        public static string SanitizeMutationName(string unsanitizedMutationName)
        {
            // NOTE: mutations and types have same naming rules, so we can re-use
            return SanitizeTypeName(unsanitizedMutationName);
        }

        public static string GenerateTraitEntityRootGraphTypeName(ITrait trait) => SanitizeTypeName("TERoot_" + trait.ID);
        public static string GenerateTraitEntityWrapperGraphTypeName(ITrait trait) => SanitizeTypeName("TEWrapper_" + trait.ID);
        public static string GenerateTraitEntityGraphTypeName(ITrait trait) => SanitizeTypeName("TE_" + trait.ID);
        public static string GenerateTraitEntityIDInputGraphTypeName(ITrait trait) => SanitizeTypeName("TE_ID_Input_" + trait.ID);
        public static string GenerateUpsertTraitEntityInputGraphTypeName(ITrait trait) => SanitizeTypeName("TE_Upsert_Input_" + trait.ID);
        public static string GenerateInsertNewMutationName(string traitID) => "insertNew_" + SanitizeMutationName(traitID);
        public static string GenerateUpdateByCIIDMutationName(string traitID) => "updateByCIID_" + SanitizeMutationName(traitID);
        public static string GenerateDeleteByCIIDMutationName(string traitID) => "deleteByCIID_" + SanitizeMutationName(traitID);
        public static string GenerateUpsertByDataIDMutationName(string traitID) => "upsertByDataID_" + SanitizeMutationName(traitID);
        public static string GenerateDeleteByDataIDMutationName(string traitID) => "deleteByDataID_" + SanitizeMutationName(traitID);
        public static string GenerateTraitAttributeFieldName(TraitAttribute ta)
        {
            // TODO: what if two unsanitized field names map to the same sanitized field name? TODO: detect this and provide a work-around
            return SanitizeFieldName(ta.Identifier);
        }
        public static string GenerateTraitRelationFieldName(TraitRelation tr)
        {
            // TODO: what if two unsanitized field names map to the same sanitized field name? TODO: detect this and provide a work-around
            return SanitizeFieldName(tr.Identifier);
        }
        public static string GenerateTraitIDFieldName(string traitID)
        {
            return SanitizeFieldName(traitID);
        }

    }
}