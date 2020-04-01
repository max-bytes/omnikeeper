import React, { useState } from "react";
import Form from 'react-bootstrap/Form';
import { Button } from 'semantic-ui-react'
import { Icon } from 'semantic-ui-react'
import { attributeType2InputProps } from '../utils/attributeTypes'

function EditableAttributeValue(props) {
  var {values, setValues, type, isArray, autoFocus, isEditable, ...rest} = props;

  isEditable = isEditable ?? true;

  if (isArray) {

    const canRemoveItem = values.length > 1;

    return <div style={{ display: 'flex', flexDirection: 'column', flexGrow: '1' }}>
        {values.map((v, index) => {
          return <Form.Group controlId={`value::${index}`} key={index} style={{display: 'flex', flexGrow: 1, alignItems: 'center'}}>
            <Form.Control disabled={!isEditable} key={index} style={{flexGrow: 1}} {...attributeType2InputProps(type)} placeholder="Enter value" value={v} 
              autoFocus={autoFocus && index === 0}
              onChange={e => {
                let newValues = values.slice();
                newValues[index] = e.target.value;
                props.setValues(newValues);
              }} />
              
            <Button className={'ml-1'} disabled={!canRemoveItem || !isEditable}  size='mini' compact onClick={e => {
              e.preventDefault();
              let newValues = values.slice();
              newValues.splice(index, 1);
              props.setValues(newValues);
            }}>
              <Icon fitted name={'remove'} size="large" />
            </Button>
            <Button className={'ml-1'} disabled={!isEditable} size='mini' compact onClick={e => {
              e.preventDefault();
              let newValues = values.slice();
              newValues.splice(index, 0, '');
              props.setValues(newValues);
            }}>
              <Icon.Group size="large">
                <Icon fitted name={'add'} />
                <Icon corner={'top right'} name='caret up' />
              </Icon.Group>
            </Button>
            <Button className={'ml-1'}  disabled={!isEditable}size='mini' compact onClick={e => {
              e.preventDefault();
              let newValues = values.slice();
              newValues.splice(index + 1, 0, '');
              props.setValues(newValues);
            }}>
              <Icon.Group size="large">
                <Icon fitted name={'add'} />
                <Icon corner name='caret down' />
              </Icon.Group>
            </Button>
          </Form.Group>;
          })
        }
    </div>;
  } else {
    return <Form.Control autoFocus={autoFocus} disabled={!isEditable} style={{flexGrow: 1}} {...attributeType2InputProps(type)} placeholder="Enter value" value={values[0]} onChange={e => setValues([e.target.value])} />;
  }
}


export default EditableAttributeValue;