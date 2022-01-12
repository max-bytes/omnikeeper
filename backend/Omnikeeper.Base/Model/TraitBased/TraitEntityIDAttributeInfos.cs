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
    public class TraitEntityIDAttributeInfos<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        private readonly IEnumerable<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)> fields;

        public TraitEntityIDAttributeInfos(IEnumerable<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)> f)
        {
            fields = f;
        }

        public IAttributeSelection GetAttributeSelectionForID()
        {
            return NamedAttributesSelection.Build(fields.Select(f => f.idAttributeName).ToHashSet());
        }

        public Guid FilterCIAttributesWithMatchingID(ID id, IDictionary<Guid, IDictionary<string, MergedCIAttribute>> ciAttributes)
        {
            (string name, IAttributeValue value)[] idAttributeValues;
            if (fields.Count() == 1)
            {
                var (_, idAttributeName, idAttributeValueType) = fields.First();
                IAttributeValue idAttributeValue = AttributeValueHelper.BuildFromTypeAndObject(idAttributeValueType, id);
                idAttributeValues = new[] { (idAttributeName, idAttributeValue) };
            }
            else
            {
                var tupleID = (ITuple)id;
                if (tupleID == null)
                    throw new Exception(); // TODO
                if (tupleID.Length != fields.Count())
                    throw new Exception(); // TODO
                idAttributeValues = fields.Select((f, index) =>
                {
                    var subIndex = tupleID[index];
                    if (subIndex == null)
                        throw new Exception(); // TODO
                    return (f.idAttributeName, AttributeValueHelper.BuildFromTypeAndObject(f.idAttributeValueType, subIndex));
                }).ToArray();
            }

            return TraitEntityHelper.FindMatchingCIIDViaAttributeValues(idAttributeValues, ciAttributes);
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
                var genericTuple = fields.Count() switch
                {
                    0 => throw new Exception("Must not happen"),
                    1 => throw new Exception("Must not happen"),
                    2 => typeof(Tuple<,>),
                    3 => typeof(Tuple<,,>),
                    4 => typeof(Tuple<,,,>),
                    5 => typeof(Tuple<,,,,>),
                    6 => typeof(Tuple<,,,,,>),
                    7 => typeof(Tuple<,,,,,,>),
                    8 => typeof(Tuple<,,,,,,,>),
                    _ => throw new Exception("Not supported")
                };
                var constructedTuple = genericTuple.MakeGenericType(fields.Select(f => f.idFieldInfo.FieldType).ToArray());
                var t = Activator.CreateInstance(constructedTuple, fields.Select(f => f.idFieldInfo.GetValue(entity)).ToArray());
                if (t == null)
                    throw new Exception(""); // TODO
                return (ID)t;
            }
        }
    }
}
