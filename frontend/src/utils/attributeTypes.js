
export const AttributeTypes = [
    {
        id: 'TEXT',
        name: 'Text'
    },
    {
        id: 'MULTILINE_TEXT',
        name: 'Multi-Line Text'
    },
    {
        id: 'INTEGER',
        name: 'Integer'
    }
];

export function attributeType2InputProps(type) {
    switch(type) {
      case 'INTEGER': return {type: 'number' };
      case 'MULTILINE_TEXT': return {type: 'text', as: 'textarea', rows: 7 };
      default: return {type: 'text' };
    }
};