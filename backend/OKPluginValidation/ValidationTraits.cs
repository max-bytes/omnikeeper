using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

namespace OKPluginValidation.Validation
{
    public static class ValidationTraits
    {
        public static readonly RecursiveTrait ValidationIssue = new RecursiveTrait("__meta.validation.validation_issue", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("validation_issue.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("message", CIAttributeTemplate.BuildFromParams("validation_issue.message", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitAttribute>() {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            },
            new List<TraitRelation>()
            {
                new TraitRelation("has_issue", new RelationTemplate("__meta.validation.has_issue", true, 1, null)),
            }
        );
        public static readonly GenericTrait ValidationIssueFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ValidationIssue);


        public static readonly RecursiveTrait Validation = new RecursiveTrait("__meta.validation.validation", new TraitOriginV1(TraitOriginType.Core),
            new List<TraitAttribute>() {
                new TraitAttribute("id", CIAttributeTemplate.BuildFromParams("validation.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("rule_name", CIAttributeTemplate.BuildFromParams("validation.rule_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
                new TraitAttribute("rule_config", CIAttributeTemplate.BuildFromParams("validation.rule_config", AttributeValueType.JSON, false)),
            },
            new List<TraitAttribute>() {
                new TraitAttribute("name", CIAttributeTemplate.BuildFromParams(ICIModel.NameAttribute, AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))),
            }
        );
        public static readonly GenericTrait ValidationFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Validation);
    }
}
