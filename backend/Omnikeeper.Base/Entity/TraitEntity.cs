using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    public abstract class TraitEntity
    {
        public readonly Guid? CIID;

        protected TraitEntity(Guid? cIID)
        {
            CIID = cIID;
        }
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

        public TraitAttributeAttribute(string taName, string aName, bool optional = false, bool isJSONSerialized = false)
        {
            this.taName = taName;
            this.aName = aName;
            this.optional = optional;
            this.isJSONSerialized = isJSONSerialized;
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
