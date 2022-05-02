﻿using Microsoft.OData.Edm;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;

namespace Omnikeeper.Controllers.OData
{
    public class EdmModelHelper
    {
        public static EdmPrimitiveTypeKind TraitAttribute2EdmPrimitiveType(TraitAttribute at)
        {
            if (at.AttributeTemplate.IsArray.GetValueOrDefault(false))
                throw new Exception("Not supported (yet)");
            var type = at.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
            return type switch
            {
                AttributeValueType.Text => EdmPrimitiveTypeKind.String,
                AttributeValueType.MultilineText => EdmPrimitiveTypeKind.String,
                AttributeValueType.Integer => EdmPrimitiveTypeKind.Int64,
                AttributeValueType.JSON => EdmPrimitiveTypeKind.String,
                AttributeValueType.YAML => EdmPrimitiveTypeKind.String,
                AttributeValueType.Image => throw new Exception("Not supported"),
                AttributeValueType.Mask => throw new Exception("Not supported"),
                AttributeValueType.Double => EdmPrimitiveTypeKind.Double,
                _ => throw new Exception("Not supported"),
            };
        }

        public static object AttributeValue2EdmValue(IAttributeValue value)
        {
            if (value.IsArray)
                throw new Exception("Not supported (yet)");
            return value switch
            {
                AttributeScalarValueText t => t.Value,
                AttributeScalarValueDouble d => d.Value,
                AttributeScalarValueInteger d => d.Value,
                AttributeScalarValueImage _ => throw new Exception("Not supported"),
                AttributeScalarValueJSON j => j.ValueStr,
                AttributeScalarValueYAML y => y.ValueStr,
                AttributeScalarValueMask _ => throw new Exception("Not supported"),
                _ => throw new Exception("Not supported"),
            };
        }
    }
}
