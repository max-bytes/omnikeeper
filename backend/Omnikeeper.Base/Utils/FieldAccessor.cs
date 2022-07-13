﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Omnikeeper.Base.Utils
{
    public class FieldAccessor
    {
        private static readonly ParameterExpression fieldParameter = Expression.Parameter(typeof(object));
        private static readonly ParameterExpression ownerParameter = Expression.Parameter(typeof(object));

        public FieldAccessor(FieldInfo field, Type ownerType)
        {
            var fieldExpression = Expression.Field(
                Expression.Convert(ownerParameter, ownerType), field);

            Get = Expression.Lambda<Func<object, object>>(
                Expression.Convert(fieldExpression, typeof(object)),
                ownerParameter).Compile();

            Set = Expression.Lambda<Action<object, object?>>(
                Expression.Assign(fieldExpression,
                    Expression.Convert(fieldParameter, field.FieldType)),
                ownerParameter, fieldParameter).Compile();
        }

        public Func<object, object> Get { get; }

        public Action<object, object?> Set { get; }
    }
}
