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
    public interface IIDAttributeInfos<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        IAttributeSelection GetAttributeSelectionForID();
        Guid FilterCIAttributesWithMatchingID(ID id, IDictionary<Guid, IDictionary<string, MergedCIAttribute>> ciAttributes);
        ID ExtractIDFromEntity(T entity);
    }

    public class SingleFieldIDAttributeInfos<T, ID> : IIDAttributeInfos<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        private readonly FieldInfo idFieldInfo;
        private readonly string idAttributeName;
        private readonly AttributeValueType idAttributeValueType;

        public SingleFieldIDAttributeInfos(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)
        {
            this.idFieldInfo = idFieldInfo;
            this.idAttributeName = idAttributeName;
            this.idAttributeValueType = idAttributeValueType;
        }

        public IAttributeSelection GetAttributeSelectionForID()
        {
            return NamedAttributesSelection.Build(idAttributeName);
        }

        public Guid FilterCIAttributesWithMatchingID(ID id, IDictionary<Guid, IDictionary<string, MergedCIAttribute>> ciAttributes)
        {
            IAttributeValue idAttributeValue = AttributeValueHelper.BuildFromTypeAndObject(idAttributeValueType, id);
            var foundCIID = ciAttributes.Where(t => t.Value[idAttributeName].Attribute.Value.Equals(idAttributeValue))
                .Select(t => t.Key)
                .OrderBy(t => t) // we order by GUID to stay consistent even when multiple CIs would match
                .FirstOrDefault();
            return foundCIID;
        }

        public ID ExtractIDFromEntity(T entity)
        {
            var id = (ID?)idFieldInfo.GetValue(entity);
            if (id == null)
                throw new Exception(); // TODO: error message
            return id;
        }
    }

    public class TupleBasedIDAttributeInfos<T, ID> : IIDAttributeInfos<T, ID> where T : TraitEntity, new() where ID : notnull
    {
        private readonly IEnumerable<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)> fields;

        public TupleBasedIDAttributeInfos(IEnumerable<(FieldInfo idFieldInfo, string idAttributeName, AttributeValueType idAttributeValueType)> f)
        {
            fields = f;
        }

        public IAttributeSelection GetAttributeSelectionForID()
        {
            return NamedAttributesSelection.Build(fields.Select(f => f.idAttributeName).ToHashSet());
        }

        public Guid FilterCIAttributesWithMatchingID(ID id, IDictionary<Guid, IDictionary<string, MergedCIAttribute>> ciAttributes)
        {
            var tupleID = (ITuple)id;
            if (tupleID == null)
                throw new Exception(); // TODO
            if (tupleID.Length != fields.Count())
                throw new Exception(); // TODO
            (string name, IAttributeValue value)[] idAttributeValues = fields.Select((f, index) => {
                var subIndex = tupleID[index];
                if (subIndex == null)
                    throw new Exception(); // TODO
                return (f.idAttributeName, AttributeValueHelper.BuildFromTypeAndObject(f.idAttributeValueType, subIndex));
            }).ToArray();
            var foundCIID = ciAttributes.Where(t =>
            {
                return idAttributeValues.All(nameValue => t.Value[nameValue.name].Attribute.Value.Equals(nameValue.value));
            })
                .Select(t => t.Key)
                .OrderBy(t => t) // we order by GUID to stay consistent even when multiple CIs would match
                .FirstOrDefault();
            return foundCIID;
        }

        public ID ExtractIDFromEntity(T entity)
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
