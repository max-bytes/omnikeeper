import React, { useState, useEffect } from "react";
import { Button, Row, Col } from "antd";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTimes, faPlus, faChevronUp, faChevronDown } from '@fortawesome/free-solid-svg-icons'
import { InputControl } from '../utils/attributeTypes'

function EditableAttributeValue(props) {
  var {values, setValues, type, isArray, autoFocus, isEditable, setHasErrors, name, controlIdSuffix , ciid } = props;
  
  isEditable = isEditable ?? true;
  controlIdSuffix = controlIdSuffix ?? "";

  var [errorsInArray, setErrorsInArray] = useState([]);
  useEffect(() => setHasErrors(errorsInArray.filter(e => e).length > 0), [errorsInArray, setHasErrors]);

  if (isArray) {
    const canRemoveItem = values.length > 1;

    return <div style={{ display: 'flex', flexDirection: 'column', flexGrow: '1', alignSelf: 'center' }}>
        {values.map((v, index) => {
          return <Row key={index} id={`value:${name}:${index}:${controlIdSuffix}`} gutter={4}>
            <Col span={19}>
                <InputControl hideNameLabel={props.hideNameLabel} attributeName={name} ciid={ciid} setHasErrors={e => {
                setErrorsInArray(oldErrorsInArray => { let newErrorsInArray = [...oldErrorsInArray]; newErrorsInArray[index] = e; return newErrorsInArray;});
                }} key={index} type={type} isArray={isArray} arrayIndex={index} value={v} disabled={!isEditable} autoFocus={autoFocus && index === 0}
                onChange={value => {
                    let newValues = values.slice();
                    newValues[index] = value;
                    props.setValues(newValues);
                }} />
            </Col>
            <Col span={1}>
                {isEditable && <Button icon={<FontAwesomeIcon icon={faTimes} />} style={{ marginLeft: "0.25rem" }} disabled={!canRemoveItem || !isEditable} onClick={e => {
                e.preventDefault();
                let newValues = values.slice();
                newValues.splice(index, 1);
                props.setValues(newValues);
                }}/>}
            </Col>
            <Col span={2}>
                {isEditable && <Button icon={<FontAwesomeIcon icon={faPlus} style={{marginRight: "10px", marginLeft: "0.25rem"}}/>} disabled={!isEditable} onClick={e => {
                e.preventDefault();
                let newValues = values.slice();
                newValues.splice(index, 0, '');
                props.setValues(newValues);
                }}><FontAwesomeIcon icon={faChevronUp} /></Button>}
            </Col>
            <Col span={2}>
                {isEditable && <Button icon={<FontAwesomeIcon icon={faPlus} style={{marginRight: "10px", marginLeft: "0.25rem"}}/>} disabled={!isEditable}  onClick={e => {
                e.preventDefault();
                let newValues = values.slice();
                newValues.splice(index + 1, 0, '');
                props.setValues(newValues);
                }}><FontAwesomeIcon icon={faChevronDown} /></Button>}
            </Col>
          </Row>
          })
        }
    </div>;
  } else {
    return <InputControl hideNameLabel={props.hideNameLabel} attributeName={name} ciid={ciid} setHasErrors={setHasErrors} 
      isArray={false} arrayIndex={0} type={type} value={values[0]} 
      disabled={!isEditable} autoFocus={autoFocus} onChange={value => setValues([value])} />
  }
}


export default EditableAttributeValue;