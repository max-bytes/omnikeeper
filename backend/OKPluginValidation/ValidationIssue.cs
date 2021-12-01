using Omnikeeper.Base.Entity;
using System;

namespace OKPluginValidation.Validation
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

        [TraitRelation("has_issue", "__meta.validation.has_issue", false, 1, -1)]
        public readonly Guid[] AffectedCIs;

        public ValidationIssue()
        {
            ID = "";
            Message = "";
            AffectedCIs = new Guid[0];
            Name = "";
        }

        public ValidationIssue(string id, string message, Guid[] affectedCIs)
        {
            ID = id;
            Message = message;
            AffectedCIs = affectedCIs;
            Name = $"Validation-Issue - {ID}";
        }
    }
}
