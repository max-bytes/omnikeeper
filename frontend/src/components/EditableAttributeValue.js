import React, { useState, useEffect } from "react";
import Form from 'react-bootstrap/Form';
import { Button } from 'semantic-ui-react'
import { Icon } from 'semantic-ui-react'
import { InputControl } from '../utils/attributeTypes'

function EditableAttributeValue(props) {
  var {values, setValues, type, isArray, autoFocus, isEditable, setHasErrors, name, controlIdSuffix} = props;
  
  isEditable = isEditable ?? true;

  var [errorsInArray, setErrorsInArray] = useState([]);
  useEffect(() => setHasErrors(errorsInArray.filter(e => e).length > 0), [errorsInArray, setHasErrors]);

  if (isArray) {
    const canRemoveItem = values.length > 1;

    return <div style={{ display: 'flex', flexDirection: 'column', flexGrow: '1', alignSelf: 'center' }}>
        {values.map((v, index) => {
          return <Form.Group controlId={`value:${name}:${index}:${controlIdSuffix}`} key={index} style={{display: 'flex', flexGrow: 1, alignItems: 'center'}}>
            <InputControl name={name + "_" + index} setHasErrors={e => {
              setErrorsInArray(oldErrorsInArray => { let newErrorsInArray = [...oldErrorsInArray]; newErrorsInArray[index] = e; return newErrorsInArray;});
            }} key={index} type={type} isArray={isArray} value={v} disabled={!isEditable} autoFocus={autoFocus && index === 0}
              onChange={value => {
                let newValues = values.slice();
                newValues[index] = value;
                props.setValues(newValues);
              }} />
              
            {isEditable && <Button className={'ml-1'} disabled={!canRemoveItem || !isEditable}  size='mini' compact onClick={e => {
              e.preventDefault();
              let newValues = values.slice();
              newValues.splice(index, 1);
              props.setValues(newValues);
            }}>
              <Icon fitted name={'remove'} size="large" />
            </Button>}
            {isEditable && <Button className={'ml-1'} disabled={!isEditable} size='mini' compact onClick={e => {
              e.preventDefault();
              let newValues = values.slice();
              newValues.splice(index, 0, '');
              props.setValues(newValues);
            }}>
              <Icon.Group size="large">
                <Icon fitted name={'add'} />
                <Icon corner={'top right'} name='caret up' />
              </Icon.Group>
            </Button>}
            {isEditable && <Button className={'ml-1'}  disabled={!isEditable} size='mini' compact onClick={e => {
              e.preventDefault();
              let newValues = values.slice();
              newValues.splice(index + 1, 0, '');
              props.setValues(newValues);
            }}>
              <Icon.Group size="large">
                <Icon fitted name={'add'} />
                <Icon corner name='caret down' />
              </Icon.Group>
            </Button>}
          </Form.Group>;
          })
        }
    </div>;
  } else {
    return <InputControl name={name} setHasErrors={setHasErrors} isArray={isArray} type={type} value={values[0]} 
      disabled={!isEditable} autoFocus={autoFocus} onChange={value => setValues([value])} />
  }
}


export default EditableAttributeValue;