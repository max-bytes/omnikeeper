using Omnikeeper.Base.Entity;
using System;

namespace OKPluginValidation
{
    [TraitEntity("__meta.validation.validation_issue", TraitOriginType.Plugin)]
    public class ValidationIssue : TraitEntity
    {
        [TraitAttribute("id", "validation_issue.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("message", "validation_issue.message")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Message;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitRelation("affected_cis", "__meta.validation.has_issue", false)]
        public readonly Guid[] AffectedCIs;

        [TraitRelation("belongs_to_validation", "__meta.validation.belongs_to_validation", true)]
        public readonly Guid[] BelongsToValidation;

        public ValidationIssue()
        {
            ID = "";
            Message = "";
            AffectedCIs = new Guid[0];
            Name = "";
            BelongsToValidation = new Guid[0];
        }

        public ValidationIssue(string id, string message, Guid[] affectedCIs, Guid validationCIID)
        {
            ID = id;
            Message = message;
            AffectedCIs = affectedCIs;
            Name = $"Validation-Issue - {ID}";
            BelongsToValidation = new Guid[] { validationCIID };
        }
    }
}
