using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class GenericTraitEntityIDAttributeInfos<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        private readonly IEnumerable<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)> fields;

        public GenericTraitEntityIDAttributeInfos(IEnumerable<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)> f)
        {
            fields = f;
        }

        public IAttributeSelection GetAttributeSelectionForID()
        {
            return NamedAttributesSelection.Build(fields.Select(f => f.idAttributeName).ToHashSet());
        }

        public string[] GetIDAttributeNames()
        {
            return fields.Select(f => f.idAttributeName).ToArray();
        }

        public IAttributeValue[] ExtractAttributeValuesFromID(ID id)
        {
            if (fields.Count() == 1)
            {
                var (_, _, idAttributeValueType) = fields.First();
                IAttributeValue idAttributeValue = AttributeValueHelper.BuildFromTypeAndObject(idAttributeValueType, id);
                return new[] { idAttributeValue };
            }
            else
            {
                var tupleID = (ITuple)id;
                if (tupleID == null)
                    throw new Exception(); // TODO
                if (tupleID.Length != fields.Count())
                    throw new Exception(); // TODO
                return fields.Select((f, index) =>
                {
                    var subIndex = tupleID[index];
                    if (subIndex == null)
                        throw new Exception(); // TODO
                    return AttributeValueHelper.BuildFromTypeAndObject(f.idAttributeValueType, subIndex);
                }).ToArray();
            }
        }

        public ID ExtractIDFromEntity(T entity)
        {
            if (fields.Count() == 1)
            {
                var (idFieldInfo, _, _) = fields.First();
                var id = (ID?)idFieldInfo.GetValue(entity);
                if (id == null)
                    throw new Exception(); // TODO: error message
                return id;
            }
            else
            {
                var t = CreateTupleFromTypeList(fields.Select(f => f.idFieldInfo.FieldType).ToArray(), fields.Select(f => f.idFieldInfo.GetValue(entity)).ToArray());
                if (t == null)
                    throw new Exception(""); // TODO
                return (ID)t;
            }
        }

        private static readonly Type[] tupleTypes = new[]
        {
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
        };
        private object? CreateTupleFromTypeList(Type[] types, object?[] values)
        {
            int numTupleMembers = types.Length;

            if (numTupleMembers <= 1 ||  numTupleMembers > 6)
                return null;

            var currentTupleType = tupleTypes[numTupleMembers - 2].MakeGenericType(types);
            var currentTuple = currentTupleType.GetConstructors()[0].Invoke(values);

            return currentTuple;
        }
    }
}
