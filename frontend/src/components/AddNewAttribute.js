import React, { useState } from "react";
import PropTypes from 'prop-types'
import { useMutation } from '@apollo/react-hooks';
import { withApollo } from 'react-apollo';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import Col from 'react-bootstrap/Col';
import { mutations } from '../graphql/mutations'
import { AttributeTypes, attributeType2InputProps } from '../utils/attributeTypes'
import { Row } from "react-bootstrap";
import EditableAttributeValue from "./EditableAttributeValue";
import { Dropdown } from 'semantic-ui-react'

function AddNewAttribute(props) {

  let initialAttribute = {name: '', type: 'TEXT', values: [''], isArray: false};
  const [selectedLayer, setSelectedLayer] = useState(undefined);
  const [newAttribute, setNewAttribute] = useState(initialAttribute);
  const [valueAutofocussed, setValueAutofocussed] = useState(false);
  React.useEffect(() => { if (!props.isEditable) setSelectedLayer(undefined); }, [props.isEditable]);

  React.useEffect(() => {if (props.prefilled) {
    setSelectedLayer(s => s ?? props.prefilled.layer);
    setNewAttribute({name: props.prefilled.name, type: props.prefilled.type, values: props.prefilled.values ?? [''], isArray: props.prefilled.isArray ?? false});
    setValueAutofocussed(true);
  }}, [props.prefilled]);

  // TODO: loading
  const [insertCIAttribute] = useMutation(mutations.INSERT_CI_ATTRIBUTE);
  const [setSelectedTimeThreshold] = useMutation(mutations.SET_SELECTED_TIME_THRESHOLD);

  if (props.visibleAndWritableLayers.length === 0) return <></>;
  
  let addButtons = <div>Add Attribute to Layer: {props.visibleAndWritableLayers.map(layer => {
    return <Button disabled={!props.isEditable} key={layer.name} style={{backgroundColor: layer.color, borderColor: layer.color, color: '#111'}} className={"mx-1"}
    onClick={() => {if (selectedLayer === layer) setSelectedLayer(undefined); else setSelectedLayer(layer);}}>{layer.name}</Button>;
  })}</div>;

  let addAttribute = <span></span>;
  if (selectedLayer) {
    addAttribute = 
      <div style={{backgroundColor: selectedLayer.color, borderColor: selectedLayer.color}} className={"p-2"}>
        <Form onSubmit={e => {
            e.preventDefault();
            insertCIAttribute({ variables: { layers: props.visibleAndWritableLayers.map(l => l.name), ciIdentity: props.ciIdentity, name: newAttribute.name, layerID: selectedLayer.id, value: {
              type: newAttribute.type,
              isArray: newAttribute.isArray,
              values: newAttribute.values
            } } }).then(d => {
              setSelectedLayer(undefined);
              setNewAttribute(initialAttribute);
              setSelectedTimeThreshold({ variables:{ newTimeThreshold: null, isLatest: true }});
            });
          }}>
          <Form.Group as={Row} controlId="type">
            <Form.Label column>Type</Form.Label>
            <Col sm={8}>
              <Dropdown placeholder='Select attribute type' fluid search selection value={newAttribute.type}
                onChange={(e, data) => {
                  // we'll clear the value, to be safe, TODO: better value migration between types
                  setNewAttribute({...newAttribute, type: data.value, value: ''});
                }}
                options={AttributeTypes.map(at => { return {key: at.id, value: at.id, text: at.name }; })}
              />
            </Col>
            <Col sm={2}>
              <Form.Group style={{height: '100%'}} controlId="isArray">
                <Form.Check style={{height: '100%'}} inline type="checkbox" label="Is Array" checked={newAttribute.isArray} onChange={e => setNewAttribute({...newAttribute, values: [''], isArray: e.target.checked })} />
              </Form.Group>
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="name">
            <Form.Label column>Name</Form.Label>
            <Col sm={10}>
              <Form.Control type="text" placeholder="Enter name" value={newAttribute.name} onChange={e => setNewAttribute({...newAttribute, name: e.target.value})} />
            </Col>
          </Form.Group>
          <Form.Group as={Row} controlId="value">
            <Form.Label column>{((newAttribute.isArray) ? 'Values' : 'Value')}</Form.Label>
            <Col sm={10}>
              <EditableAttributeValue autoFocus={valueAutofocussed} values={newAttribute.values} setValues={vs => setNewAttribute({...newAttribute, values: vs})} type={newAttribute.type} isArray={newAttribute.isArray} />

              {/* <Form.Control autoFocus={valueAutofocussed} {...attributeType2InputProps(newAttribute.type)} placeholder="Enter value" value={newAttribute.value} onChange={e => setNewAttribute({...newAttribute, value: e.target.value})} />                         */}
            </Col>
          </Form.Group>
          <Button variant="primary" type="submit">Insert</Button>
        </Form>
      </div>;
  }

  return <div className={"m-2"}>
    {addButtons}
    {addAttribute}
    </div>;
}

AddNewAttribute.propTypes = {
  isEditable: PropTypes.bool.isRequired,
  ciIdentity: PropTypes.string.isRequired
}

export default withApollo(AddNewAttribute);