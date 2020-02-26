
export const AttributeTypes = [
    {
        id: 'text',
        typename: 'AttributeValueTextType',
        name: 'Text'
    },
    {
        id: 'integer',
        typename: 'AttributeValueIntegerType',
        name: 'Integer'
    }
];

export function attributeID2Object(id) {
    return AttributeTypes.find(at => at.id === id);
};

export function attributeTypename2Object(typename) {
    return AttributeTypes.find(at => at.typename === typename);
};

export function attribute2InputType(attribute) {
    switch(attribute.id) {
      case 'integer': return 'number';
      default: return 'text';
    }
};