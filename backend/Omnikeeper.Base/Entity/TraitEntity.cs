using System;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    // TODO: needed?
    public abstract class TraitEntity
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TraitEntityAttribute : Attribute
    {
        public readonly string traitName;
        public readonly TraitOriginType originType;

        public TraitEntityAttribute(string traitName, TraitOriginType originType)
        {
            this.traitName = traitName;
            this.originType = originType;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeAttribute : Attribute
    {
        public readonly string taName;
        public readonly string aName;
        public readonly bool optional;
        public readonly bool isJSONSerialized;
        public readonly bool multilineTextHint;

        public TraitAttributeAttribute(string taName, string aName, bool optional = false, bool isJSONSerialized = false, bool multilineTextHint = false)
        {
            this.taName = taName;
            this.aName = aName;
            this.optional = optional;
            this.isJSONSerialized = isJSONSerialized;
            this.multilineTextHint = multilineTextHint;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitRelationAttribute : Attribute
    {
        public readonly string trName;
        public readonly string predicateID;
        public readonly bool directionForward;
        public readonly int? minCardinality;
        public readonly int? maxCardinality;

        public TraitRelationAttribute(string trName, string predicateID, bool directionForward, int minCardinality, int maxCardinality)
        {
            this.trName = trName;
            this.predicateID = predicateID;
            this.predicateID = predicateID;
            this.directionForward = directionForward;

            if (minCardinality == -1)
                this.minCardinality = null;
            else
                this.minCardinality = minCardinality;
            if (maxCardinality == -1)
                this.maxCardinality = null;
            else
                this.maxCardinality = maxCardinality;
        }
    }



    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitEntityIDAttribute : Attribute
    {
        public TraitEntityIDAttribute()
        {
        }
    }



    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeValueConstraintTextLengthAttribute : Attribute
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public TraitAttributeValueConstraintTextLengthAttribute(int minimum, int maximum)
        {
            if (minimum == -1)
                Minimum = null;
            else
                Minimum = minimum;
            if (maximum == -1)
                Maximum = null;
            else
                Maximum = maximum;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeValueConstraintTextRegexAttribute : Attribute
    {
        public readonly string RegexStr;
        public readonly RegexOptions RegexOptions;

        public TraitAttributeValueConstraintTextRegexAttribute(string regexStr, RegexOptions regexOptions)
        {
            RegexStr = regexStr;
            RegexOptions = regexOptions;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TraitAttributeValueConstraintArrayLengthAttribute : Attribute
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public TraitAttributeValueConstraintArrayLengthAttribute(int minimum, int maximum)
        {
            if (minimum == -1)
                Minimum = null;
            else
                Minimum = minimum;
            if (maximum == -1)
                Maximum = null;
            else
                Maximum = maximum;
        }
    }
}
